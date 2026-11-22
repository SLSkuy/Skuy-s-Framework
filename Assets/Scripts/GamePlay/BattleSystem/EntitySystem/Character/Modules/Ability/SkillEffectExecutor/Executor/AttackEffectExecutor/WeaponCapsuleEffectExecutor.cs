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
            _weaponAnchor = _context.Anchor.transform;
        }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is WeaponCapsuleSkillEffect weaponEffect)
            {
                _height = weaponEffect.weaponHeight;
                _radius = weaponEffect.weaponRadius;
                _durationTicks = weaponEffect.durationTicks;
                uint maxSamples = System.Math.Max(1u, _durationTicks / 2);
                _sampleCount = System.Math.Min((uint)Mathf.Max(1, weaponEffect.hitRate), maxSamples);
                _sampleIndex = 0;
                _elapsedTicks = 0;
                _nextSampleTick = 0;

                _isAttacking = _durationTicks > 0;
            }
        }

        public override void Tick(float deltaTime)
        {
            if (!_isAttacking || _sampleIndex == _sampleCount) return;
            if (_elapsedTicks++ != _nextSampleTick) return;

            // 从窗口首 Tick 开始，将配置的检测次数分布在整个窗口内。
            _sampleIndex++;
            
            Vector3 direction = _weaponAnchor.forward;
            Vector3 start = _weaponAnchor.position + direction * _radius;
            Vector3 end = start + direction * Mathf.Max(0f, _height - 2f * _radius);

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
    }
}