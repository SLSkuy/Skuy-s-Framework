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
        
        #region 属性
        public override ModuleType ModuleType => ModuleType.Ability;

        /// <summary> 当前是否正在释放技能 </summary>
        public bool IsCasting { get; private set; }
        public BaseSkill CastingSkill { get; private set; }
        
        /// <summary> 技能当前持续时间 </summary>
        public float SkillRemainingTime { get; private set; }
        #endregion

        #region 技能管理

        public bool TryCastSkill(uint skillId)
        {
            if (_config.TryGetSkillConfig(skillId, out BaseSkill skillConfig))
            {
                if (IsCasting) return false;
                
                IsCasting = true;
                CastingSkill = skillConfig;
                
                // TODO: 优化动画调用逻辑
                _animation.PlayActionAnimation(new EntityAnimationCommand
                {
                    clip = CastingSkill.clip,
                    duration = CastingSkill.duration,
                });
                
                return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            if (SkillRemainingTime < CastingSkill.duration)
            {
                SkillRemainingTime += deltaTime;
            }
            else
            {
                SkillRemainingTime = 0f;
                IsCasting = false;
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
                skillId = CastingSkill.skillId,
                remainTime = SkillRemainingTime,
            };
        }

        public override void RestoreSnapshot(in AbilitySnapshot state)
        {
            if (_config.TryGetSkillConfig(state.skillId, out BaseSkill skillConfig))
            {
                CastingSkill = skillConfig;
                SkillRemainingTime = state.remainTime;
                
                _animation.PlayActionAnimation(new EntityAnimationCommand
                {
                    clip = CastingSkill.clip,
                    duration = CastingSkill.duration,
                }, SkillRemainingTime);
            }
        }
    }
}