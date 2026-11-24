namespace GamePlay.BattleSystem
{
    /// <summary>技能结束原因；成功派生不会清除连招进度。</summary>
    public enum SkillEndCode
    {
        /// <summary>
        /// 未知原因
        /// </summary>
        None,
        
        /// <summary>
        /// 技能释放完毕
        /// </summary>
        Completed,
        
        /// <summary>
        /// 技能派生中断
        /// </summary>
        Replaced,
        
        /// <summary>
        /// 技能中断
        /// </summary>
        Interrupted,
        
        /// <summary>
        /// 技能重置
        /// </summary>
        Reset
    }
}