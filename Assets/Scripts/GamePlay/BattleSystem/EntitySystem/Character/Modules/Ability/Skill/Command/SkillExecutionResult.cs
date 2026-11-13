namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能释放结果，记录技能、结果码以及未通过的条件
    /// </summary>
    public readonly struct SkillExecutionResult
    {
        #region 属性
        /// <summary>
        /// 本次请求释放的技能ID
        /// </summary>
        public uint SkillId { get; }
        
        /// <summary>
        /// 释放结果码
        /// </summary>
        public SkillExecutionCode Code { get; }
        
        /// <summary>
        /// 失败的条件
        /// </summary>
        // public ICondition FailedCondition { get; }
        
        public bool IsSuccess => Code == SkillExecutionCode.Success;
        #endregion

        // public SkillExecutionResult(uint skillId, SkillExecutionCode code, ICondition failedCondition = null)
        public SkillExecutionResult(uint skillId, SkillExecutionCode code)
        {
            SkillId = skillId;
            Code = code;
            // FailedCondition = failedCondition;
        }
    }
}