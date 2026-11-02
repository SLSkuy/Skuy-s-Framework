using Framework;
using GamePlay.DataProxy;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 角色能力组件，控制角色的各种战斗能力
    /// 外部通过传入技能ID，索引技能库实现技能释放
    /// </summary>
    public class AbilityModule : EntitySnapshotModuleBase<AbilitySnapshot>
    {
        private readonly AbilityConfigProxy _config = Global.GetDataProxy<AbilityConfigProxy>();

        private AnimationModule _animation;
        
        /// <summary>
        /// 当前释放的技能信息
        /// </summary>
        private BaseSkill _castingSkill;
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Ability;
        
        /// <summary> 当前是否正在释放技能 </summary>
        public bool IsCasting => _castingSkill != null;
        /// <summary> 技能当前持续时间 </summary>
        public float SkillRemainingTime { get; private set; }
        #endregion

        #region 技能管理

        public bool TryCastSkill(uint skillId)
        {
            if (_config.TryGetSkillConfig(skillId, out BaseSkill skillConfig))
            {
                if (IsCasting) return false;
                
                _castingSkill = skillConfig;
                
                // TODO: 优化动画调用逻辑
                _animation.PlayActionAnimation(new EntityAnimationCommand
                {
                    clip = _castingSkill.clip,
                    duration = _castingSkill.duration,
                });
                
                return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            if (IsCasting && SkillRemainingTime < _castingSkill.duration)
            {
                SkillRemainingTime += deltaTime;
            }
            else
            {
                SkillRemainingTime = 0f;
                _castingSkill = null;
            }
        }

        #endregion

        protected override void OnBound(EntityCharacter target)
        {
            _animation = Context.Get<AnimationModule>();
        }

        public override AbilitySnapshot CaptureSnapshot()
        {
            // 没有释放的技能信息，传递空数据
            if (!IsCasting)
            {
                return default;
            }
            
            return new AbilitySnapshot
            {
                skillId = _castingSkill.skillId,
                remainTime = SkillRemainingTime,
            };
        }

        public override void RestoreSnapshot(in AbilitySnapshot state)
        {
            if (_config.TryGetSkillConfig(state.skillId, out BaseSkill skillConfig))
            {
                _castingSkill = skillConfig;
                SkillRemainingTime = state.remainTime;
                
                _animation.PlayActionAnimation(new EntityAnimationCommand
                {
                    clip = _castingSkill.clip,
                    duration = _castingSkill.duration,
                }, SkillRemainingTime);
            }
        }
        
        public override void Reset()
        {
            _castingSkill = null;
            SkillRemainingTime = 0f;
        }
    }
}