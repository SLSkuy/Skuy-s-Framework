using Framework;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟核：由对局流程态按会话角色创建，负责时钟与实体会话。
    /// </summary>
    public interface ISimulationKernel : ISubSystem
    {
        Simulator SimulationKernal { get; }
        bool IsSessionRunning { get; }

        bool StartSession();
        void StopSession();
    }
}
