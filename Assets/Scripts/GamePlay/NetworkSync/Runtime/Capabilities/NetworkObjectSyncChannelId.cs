namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络对象能力使用的逻辑同步通道标识。
    /// </summary>
    public enum NetworkObjectSyncChannelId
    {
        TransformSnapshot = 1,
        InputCommand = 2,
        SimulationState = 3
    }
}
