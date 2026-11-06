namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能效果结束策略
    /// </summary>
    public enum SkillEffectEndPolicy
    {
        /// <summary>
        /// 以技能结束同步结束
        /// </summary>
        WithSkill, 
        
        /// <summary>
        /// 效果拥有独立的生命周期
        /// </summary>
        Independent
    }
}