namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 运动技能碰撞时行为策略
    /// </summary>
    public enum SkillLocomotionCollisionPolicy
    {
        /// <summary>
        /// 忽视碰撞阻挡，继续移动
        /// </summary>
        IgnoreBlocked,
        
        /// <summary>
        /// 被阻挡时停止移动
        /// </summary>
        StopOnBlocked
    }
}