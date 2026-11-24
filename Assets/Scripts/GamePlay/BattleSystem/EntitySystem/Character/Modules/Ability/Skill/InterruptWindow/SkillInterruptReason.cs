namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能中断方式
    /// </summary>
    public enum SkillInterruptReason
    {
        /// <summary>
        /// 移动打断
        /// </summary>
        Locomotion,
        
        /// <summary>
        /// 跳跃打断
        /// </summary>
        Jump,
        
        /// <summary>
        /// 闪避打断
        /// </summary>
        Dodge,
        
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