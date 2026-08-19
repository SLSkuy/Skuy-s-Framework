namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络对象基础能力标识。
    /// </summary>
    public enum NetworkObjectCapabilityId
    {
        Transform = 1,
        Simulation = 2,
        Snapshot = 3,
        Prediction = 4,
        Interpolation = 5
    }
}
