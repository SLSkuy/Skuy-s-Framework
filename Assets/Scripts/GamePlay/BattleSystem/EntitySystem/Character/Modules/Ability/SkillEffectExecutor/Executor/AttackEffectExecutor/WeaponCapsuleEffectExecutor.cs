using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class WeaponCapsuleEffectExecutor : BaseSkillEffectExecutor
    {
        /// <summary>当前命中窗口中已结算的实体角色</summary>
        private readonly HashSet<EntityCharacter> _hits = new();

        private readonly Transform _weaponAnchor;
        private Collider[] _overlapResults = new Collider[32];

#if UNITY_EDITOR
        private readonly Queue<(Vector3 Start, Vector3 End, Quaternion Rotation, float Radius, float VisibleUntil)>
            _queryGizmos = new();
#endif

        // 武器攻击范围信息
        private float _height;
        private float _radius;
        
        // 攻击次数
        private uint _sampleCount;
        private uint _sampleIndex;
        private uint _nextSampleTick;
        
        // Tick计数器
        private uint _durationTicks;
        private uint _elapsedTicks;
        
        private bool _isAttacking;

        public WeaponCapsuleEffectExecutor(EntityContext context) : base(context)
        {
            WeaponAnchor anchor = context.Anchor;
            _weaponAnchor = anchor ? anchor.transform : null;
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is WeaponCapsuleSkillEffect weaponEffect)
            {
                _height = weaponEffect.weaponHeight;
                _radius = weaponEffect.weaponRadius;
                _durationTicks = weaponEffect.durationTicks;
                
                uint maxSamples = System.Math.Max(1u, _durationTicks);
                _sampleCount = System.Math.Min((uint)Mathf.Max(1, weaponEffect.hitRate), maxSamples);
                _sampleIndex = 0;
                _elapsedTicks = 0;
                _nextSampleTick = 0;

                _isAttacking = _weaponAnchor && _durationTicks > 0;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (!_isAttacking || _sampleIndex == _sampleCount) return;
            if (_elapsedTicks++ != _nextSampleTick) return;

            // 从窗口首 Tick 开始，将配置的检测次数分布在整个窗口内。
            _sampleIndex++;
            _nextSampleTick = (uint)((ulong)_sampleIndex * _durationTicks / _sampleCount);

            GetCapsuleEndpoints(_weaponAnchor, _height, _radius, out Vector3 start, out Vector3 end);

#if UNITY_EDITOR
            float now = Time.unscaledTime;
            RemoveExpiredGizmos(now);
            _queryGizmos.Enqueue((start, end, _weaponAnchor.rotation, _radius, now + GlobalConst.GIZMOS_VISIBLE_TIME));
#endif

            // 查询前提交已写入的碰撞体位姿，避免应用的Tick效果还没有应用到物理引擎上
            Physics.SyncTransforms();

            int hitCount;
            while (true)
            {
                int layerMask = GlobalConst.EnemyLayerMask;
                hitCount = Physics.OverlapCapsuleNonAlloc(start, end, _radius, _overlapResults,
                    layerMask, QueryTriggerInteraction.Collide);
                if (hitCount < _overlapResults.Length) break;

                _overlapResults = new Collider[_overlapResults.Length * 2];
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _overlapResults[i];
                _overlapResults[i] = null;

                if (!collider.TryGetComponent(out HurtBox hurtBox)) continue;
                EntityCharacter target = hurtBox.Owner;
                if (!_hits.Add(target)) continue;   // 已经处理过了

                // TODO: 替换临时伤害结算算法
                target.Context.Get<AttributeModule>().Damage(_context.Config.attributeConfig.atk);
            }
        }

        public override void StopExecution()
        {
            _isAttacking = false;
            _hits.Clear();
        }

        /// <summary>按挂点的局部 +Z 方向计算胶囊两端的世界空间球心</summary>
        public static void GetCapsuleEndpoints(Transform anchor, float height, float radius,
            out Vector3 start, out Vector3 end)
        {
            Vector3 direction = anchor.forward;
            start = anchor.position + direction * radius;
            end = start + direction * Mathf.Max(0f, height - 2f * radius);
        }

#if UNITY_EDITOR
        internal void DrawGizmos()
        {
            RemoveExpiredGizmos(Time.unscaledTime);
            if (_queryGizmos.Count == 0) return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.identity;
            foreach (var query in _queryGizmos)
            {
                DrawWireCapsule(query.Start, query.End, query.Rotation, query.Radius, Gizmos.DrawLine);
            }
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        /// <summary>输出胶囊轮廓线段，供场景 Gizmos 和技能编辑器预览使用。</summary>
        public static void DrawWireCapsule(Vector3 start, Vector3 end, Quaternion rotation, float radius,
            System.Action<Vector3, Vector3> drawLine)
        {
            Vector3 right = rotation * Vector3.right * radius;
            Vector3 up = rotation * Vector3.up * radius;
            Vector3 forward = rotation * Vector3.forward * radius;

            DrawArc(start, right, up, 2f * Mathf.PI, drawLine);
            DrawArc(end, right, up, 2f * Mathf.PI, drawLine);
            DrawArc(start, right, -forward, Mathf.PI, drawLine);
            DrawArc(start, up, -forward, Mathf.PI, drawLine);
            DrawArc(end, right, forward, Mathf.PI, drawLine);
            DrawArc(end, up, forward, Mathf.PI, drawLine);
            drawLine(start + right, end + right);
            drawLine(start - right, end - right);
            drawLine(start + up, end + up);
            drawLine(start - up, end - up);
        }

        private static void DrawArc(Vector3 center, Vector3 firstAxis, Vector3 secondAxis, float sweep,
            System.Action<Vector3, Vector3> drawLine)
        {
            const int segments = 16;
            Vector3 previous = center + firstAxis;
            for (int i = 1; i <= segments; i++)
            {
                float angle = sweep * i / segments;
                Vector3 point = center + firstAxis * Mathf.Cos(angle) + secondAxis * Mathf.Sin(angle);
                drawLine(previous, point);
                previous = point;
            }
        }

        private void RemoveExpiredGizmos(float now)
        {
            while (_queryGizmos.Count > 0 && _queryGizmos.Peek().VisibleUntil <= now)
            {
                _queryGizmos.Dequeue();
            }
        }
#endif
    }
}
