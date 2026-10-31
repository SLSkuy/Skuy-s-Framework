namespace GamePlay.BattleSystem
{
    public enum InteractType
    {
        /// <summary>
        /// 本地即时交互，不需要上发检验
        /// </summary>
        Instant,
        
        /// <summary>
        /// 需要进行检验的交互物品，交互时发送上报
        /// </summary>
        Inspection
    }
}