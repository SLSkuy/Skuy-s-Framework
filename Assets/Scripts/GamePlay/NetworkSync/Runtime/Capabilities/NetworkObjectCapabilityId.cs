namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络对象基础能力标识。
    /// </summary>
    public enum NetworkObjectCapabilityId
    {
        Transform = 1,
        InputCommand = 2,
        Simulation = 3,
        Snapshot = 4,
        Prediction = 5,
        Interpolation = 6
    }
}
