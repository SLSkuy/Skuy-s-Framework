namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 连招转换条件检测结果
    /// </summary>
    public readonly struct TransitionCheckResult
    {
        #region 属性
        public TransitionCheckCode Code { get; }
        public ICombatGraphNode TargetNode { get; }
        
        public bool IsSuccess => Code == TransitionCheckCode.Success;
        #endregion

        public TransitionCheckResult(TransitionCheckCode code, ICombatGraphNode targetNode = null)
        {
            Code = code;
            TargetNode = targetNode;
        }
    }
}