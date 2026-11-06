namespace GamePlay.BattleSystem
{
    public abstract class BaseSkillEffectExecutor : ISkillEffectExecutor
    {
        protected EntityContext _context;

        protected BaseSkillEffectExecutor(EntityContext context) { _context = context; }
        
        /// <summary>
        /// 执行技能效果
        /// </summary>
        public abstract void ExecuteSkillEffect(SkillEffect effect);

        /// <summary>
        /// 结束/中断技能效果的执行
        /// </summary>
        public abstract void StopExecution();
        
        public virtual void Tick(float deltaTime) { }
    }
}