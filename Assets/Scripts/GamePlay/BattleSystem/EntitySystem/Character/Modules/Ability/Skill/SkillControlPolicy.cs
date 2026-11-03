namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 技能期间控制命令管理策略
    /// </summary>
    public enum SkillControlPolicy
    {
        /// <summary>
        /// 不禁用控制
        /// </summary>
        None = 1,
        
        /// <summary>
        /// 禁止角色视角控制
        /// </summary>
        LockView = 1 << 1,
        
        /// <summary>
        /// 禁止角色转向控制
        /// </summary>
        LockMeshRotation = 1 << 2,
        
        /// <summary>
        /// 禁止角色移动控制
        /// </summary>
        LockMovement = 1 << 3,
    }
}