namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能动画效果处理器，处理技能释放期间的所有角色动画播放
    /// </summary>
    public class AnimationEffectExecutor : BaseSkillEffectExecutor
    {
        public AnimationEffectExecutor(AbilityModule module) : base(module) { }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is AnimationSkillEffect animationEffect)
            {
                
            }
        }
    }
}

