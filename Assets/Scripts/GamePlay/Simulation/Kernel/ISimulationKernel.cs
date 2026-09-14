using Framework;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟核：由玩法粘合点按会话角色创建，负责时钟与实体会话。
    /// </summary>
    public interface ISimulationKernel : ISubSystem
    {
        Simulation.Simulator Simulator { get; }
        bool IsSessionRunning { get; }

        bool StartSession();
        void StopSession();
    }
}
