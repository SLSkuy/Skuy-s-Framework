namespace GamePlay.Simulator
{
    /// <summary>
    /// 实体对象身份枚举
    /// </summary>
    public enum EntityObjectRole
    {
        /// <summary>
        /// 消耗输入命令进行模拟
        /// </summary>
        Authority,

        /// <summary>
        /// 本地预测，接收快照进行调整
        /// </summary>
        Predict,

        /// <summary>
        /// 消耗状态快照进行模拟
        /// </summary>
        Replica,
        
        /// <summary>
        /// 单机游玩
        /// </summary>
        LocalPlay,
    }
}
