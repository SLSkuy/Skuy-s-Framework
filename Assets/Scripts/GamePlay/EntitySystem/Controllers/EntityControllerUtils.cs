using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 旧调用点的命令构建兼容工具。
    /// </summary>
    internal static class EntityControllerUtils
    {
        /// <summary>
        /// 构建命令并通过统一模拟入口执行。
        /// </summary>
        public static void Step(IEntitySimulation simulation, EntityInputCommandBuilder commandBuilder,
            uint tick, float deltaTime, in InputState input)
        {
            EntityInputCommand command = commandBuilder.Build(tick, input);
            simulation.Step(tick, deltaTime, command);
        }
    }
}
