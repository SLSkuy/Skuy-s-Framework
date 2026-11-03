namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动技能效果处理器，处理技能释放期间的角色位移效果
    /// </summary>
    public class LocomotionEffectExecutor : BaseSkillEffectExecutor
    {
        public LocomotionEffectExecutor(AbilityModule module) : base(module) { }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is MoveSkillEffect moveEffect)
            {
                
            }

            if (effect is RootMotionSkillEffect rootMotionEffect)
            {
                
            }
        }
    }
}