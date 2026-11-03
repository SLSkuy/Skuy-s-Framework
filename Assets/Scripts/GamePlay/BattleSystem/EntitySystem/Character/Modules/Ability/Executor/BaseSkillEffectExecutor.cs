namespace GamePlay.BattleSystem
{
    public abstract class BaseSkillEffectExecutor : ISkillEffectExecutor
    {
        protected AbilityModule _abilityModule;

        protected BaseSkillEffectExecutor(AbilityModule module) { _abilityModule = module; }
        
        /// <summary>
        /// 执行技能效果
        /// </summary>
        public abstract void ExecuteSkillEffect(SkillEffect effect);
    }
}