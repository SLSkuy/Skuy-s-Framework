namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能释放结果，记录技能、结果码以及未通过的条件
    /// </summary>
    public readonly struct SkillExecutionCheckResult
    {
        #region 属性
        /// <summary>
        /// 本次请求释放的技能ID
        /// </summary>
        public uint SkillId { get; }
        
        /// <summary>
        /// 释放结果码
        /// </summary>
        public SkillExecutionCheckCode Code { get; }
        
        /// <summary>
        /// 失败的条件
        /// </summary>
        // public ICondition FailedCondition { get; }
        
        public bool IsSuccess => Code is SkillExecutionCheckCode.Success or SkillExecutionCheckCode.ReplaceAllowed;
        #endregion

        // public SkillExecutionResult(uint skillId, SkillExecutionCode code, ICondition failedCondition = null)
        public SkillExecutionCheckResult(uint skillId, SkillStateCheckResult code)
        {
            SkillId = skillId;
            // FailedCondition = failedCondition;

            switch (code)
            {
                case SkillStateCheckResult.Casting:
                    Code = SkillExecutionCheckCode.Casting;
                    break;
                case SkillStateCheckResult.SkillLocked:
                    Code = SkillExecutionCheckCode.SkillLocked;
                    break;
                case SkillStateCheckResult.Cooldown:
                    Code = SkillExecutionCheckCode.Cooldown;
                    break;
                case SkillStateCheckResult.ConditionFailed:
                    Code = SkillExecutionCheckCode.ConditionFailed;
                    break;
                default:
                    Code = SkillExecutionCheckCode.None;
                    break;
            }
        }

        public SkillExecutionCheckResult(uint skillId, SkillExecutionCheckCode code)
        {
            SkillId = skillId;
            Code = code;
        }
    }
}