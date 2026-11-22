using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class HitBoxEffectExecutor : BaseSkillEffectExecutor
    {
        /// <summary>当前命中窗口中已结算的实体角色</summary>
        private readonly HashSet<EntityCharacter> _hits = new();
        
        private readonly MovementModule _movement;
        private Collider[] _overlapResults = new Collider[32];

#if UNITY_EDITOR
        private readonly Queue<(Matrix4x4 Matrix, float VisibleUntil)> _queryGizmos = new();
#endif

        private Vector3 _hitBoxPivot;
        private Quaternion _hitBoxRotation;
        private Vector3 _hitBoxSize;
        private bool _isAttacking;

        public HitBoxEffectExecutor(EntityContext context) : base(context)
        {
            _movement = context.Get<MovementModule>();
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is HitBoxSkillEffect hitBoxEffect)
            {
                _hitBoxPivot = hitBoxEffect.hitBoxPivot;
                _hitBoxRotation = hitBoxEffect.hitBoxRotation;
                _hitBoxSize = hitBoxEffect.hitBoxSize;

                _isAttacking = true;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (!_isAttacking) return;

            Quaternion characterRotation = _movement.Rotation;
            Vector3 center = _movement.Position + characterRotation * _hitBoxPivot;
            Quaternion rotation = characterRotation * _hitBoxRotation;
            Vector3 halfExtents = _hitBoxSize * 0.5f;

#if UNITY_EDITOR
            // 每次查询的位姿单独保留，便于观察只有一个 Tick 的命中窗口。
            float now = Time.unscaledTime;
            RemoveExpiredGizmos(now);
            _queryGizmos.Enqueue((Matrix4x4.TRS(center, rotation, _hitBoxSize), now + GlobalConst.GIZMOS_VISIBLE_TIME));
#endif

            // 查询前提交已写入的碰撞体位姿，避免应用的Tick效果还没有应用到物理引擎上
            Physics.SyncTransforms();

            int hitCount = 0;
            while (true)
            {
                // 使用全局定义敌人碰撞盒层级
                int layerMask = GlobalConst.EnemyLayerMask;
                hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapResults, rotation, 
                    layerMask, QueryTriggerInteraction.Collide);
                if (hitCount < _overlapResults.Length) break;

                // 缓冲区写满时结果可能被截断，扩容后重新查询。
                _overlapResults = new Collider[_overlapResults.Length * 2];
            }

            // 处理伤害范围检测到的所有实体
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

#if UNITY_EDITOR
        internal void DrawGizmos()
        {
            RemoveExpiredGizmos(Time.unscaledTime);
            if (_queryGizmos.Count == 0) return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            foreach (var query in _queryGizmos)
            {
                Gizmos.matrix = query.Matrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
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
