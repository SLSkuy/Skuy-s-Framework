namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 模拟接口定义
    /// </summary>
    public interface IEntitySimulation
    {
        /// <summary>
        /// 按Tick步进模拟
        /// </summary>
        void Step(uint tick, float deltaTime, in EntityCommand command);
    }
}
