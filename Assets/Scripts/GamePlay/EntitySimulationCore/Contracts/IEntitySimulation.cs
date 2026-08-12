namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 单机、预测与权威模式共用的实体模拟入口。
    /// </summary>
    public interface IEntitySimulation
    {
        /// <summary>
        /// 使用指定 Tick 的完整命令推进一次固定步长模拟。
        /// </summary>
        void Step(uint tick, float deltaTime, in EntityInputCommand command);
    }
}
