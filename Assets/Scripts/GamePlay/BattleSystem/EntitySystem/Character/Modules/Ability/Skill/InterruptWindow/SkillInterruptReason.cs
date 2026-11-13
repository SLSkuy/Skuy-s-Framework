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
        /// 翻滚打断
        /// </summary>
        Roll,
        
        /// <summary>
        /// 角色受击（普通攻击）
        /// </summary>
        Hit,
        
        /// <summary>
        /// 角色受击（暴击/强力攻击）
        /// </summary>
        Critical,
        
        /// <summary>
        /// 技能派生
        /// </summary>
        Replace,
    }
}