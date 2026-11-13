namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能释放请求的结果码，成功仅表示已开始释放
    /// </summary>
    public enum SkillExecutionCheckCode
    {
        /// <summary>
        /// 未知原因
        /// </summary>
        None,
        
        /// <summary>
        /// 技能检测释放通过，可以进行释放
        /// </summary>
        Success,
        
        /// <summary>
        /// 技能已经释放
        /// </summary>
        Casting,
        
        /// <summary>
        /// 不存在对应技能
        /// </summary>
        SkillNotFound,
        
        /// <summary>
        /// 技能未解锁
        /// </summary>
        SkillLocked,

        /// <summary>
        /// 技能冷却中
        /// </summary>
        Cooldown,
        
        /// <summary>
        /// 条件检测失败
        /// </summary>
        ConditionFailed,

        /// <summary>
        /// 当前技能不处于允许替换的窗口
        /// </summary>
        ReplaceNotAllowed
    }
}