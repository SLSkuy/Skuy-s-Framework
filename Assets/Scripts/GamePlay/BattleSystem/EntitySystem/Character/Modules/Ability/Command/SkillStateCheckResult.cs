namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能检测状态
    /// </summary>
    public enum SkillStateCheckResult
    {
        /// <summary>
        /// 未知原因
        /// </summary>
        None,
        
        /// <summary>
        /// 技能准备完毕
        /// </summary>
        Ready,
        
        /// <summary>
        /// 技能正在释放中
        /// </summary>
        Casting,
        
        /// <summary>
        /// 技能未解锁
        /// </summary>
        SkillLocked,
        
        /// <summary>
        /// 技能冷却中
        /// </summary>
        Cooldown,
        
        /// <summary>
        /// 技能条件不满足
        /// </summary>
        ConditionFailed,
    }
}