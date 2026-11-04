namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 一次施法的状态，完成和中断后不能继续调度
    /// </summary>
    public enum SkillState
    {
        /// <summary>
        /// 待机状态，技能随时可以触发
        /// </summary>
        Idle,
        
        /// <summary>
        /// 释放中
        /// </summary>
        Casting,
        
        /// <summary>
        /// 冷却中
        /// </summary>
        CoolingDown,
        
        /// <summary>
        /// 锁定中
        /// </summary>
        Locked,
    }
}