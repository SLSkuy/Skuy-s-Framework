namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能结束结果，技能技能结束的原因等信息
    /// </summary>
    public readonly struct SkillEndResult
    {
        #region 属性
        /// <summary>
        /// 本次结束技能ID
        /// </summary>
        public uint SkillId { get; }
        
        /// <summary>
        /// 结束原因
        /// </summary>
        public SkillEndCode Code { get; }
        #endregion

        public SkillEndResult(uint skillId, SkillEndCode code)
        {
            SkillId = skillId;
            Code = code;
        }
    }
}