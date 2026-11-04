namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动技能效果处理器，处理技能释放期间的角色位移效果
    /// </summary>
    public class LocomotionEffectExecutor : BaseSkillEffectExecutor
    {
        public LocomotionEffectExecutor(EntityContext context) : base(context) { }

        public override void ExecuteSkillEffect(SkillEffect effect)
        {
            if (effect is MoveSkillEffect moveEffect)
            {
                
            }

            if (effect is RootMotionSkillEffect rootMotionEffect)
            {
                
            }
        }

        public override void StopExecution()
        {
            
        }
    }
}