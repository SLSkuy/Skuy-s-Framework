namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体模拟入口接口，用于隔离 Unity 帧驱动与外部 Tick 驱动。
    /// </summary>
    public interface IEntitySimulation
    {
        /// <summary>
        /// 是否由外部 Tick 驱动模拟。
        /// </summary>
        bool TickDrive { get; set; }

        /// <summary>
        /// 推进实体模拟。
        /// </summary>
        void Simulate(float deltaTime);
    }
}

