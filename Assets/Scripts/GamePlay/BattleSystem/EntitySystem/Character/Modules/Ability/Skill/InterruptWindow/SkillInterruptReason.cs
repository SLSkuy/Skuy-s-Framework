namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能中断方式
    /// </summary>
    public enum SkillInterruptReason
    {
        /// <summary>
        /// 技能取消
        /// </summary>
        Cancel, 
        
        /// <summary>
        /// 角色受击
        /// </summary>
        Hit, 
        
        /// <summary>
        /// 技能派生
        /// </summary>
        Replace,
    }
}