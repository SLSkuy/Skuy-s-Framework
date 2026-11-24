namespace GamePlay.BattleSystem
{
    public enum TransitionCheckCode
    {
        /// <summary>
        /// 未知原因
        /// </summary>
        None,
        
        /// <summary>
        /// 某一条转换条件检测通过，可以进行转换（只是转换条件满足，不代表释放条件满足）
        /// </summary>
        Success,
        
        /// <summary>
        /// 所有转换条件都不满足
        /// </summary>
        ConditionFailed,
    }
}