using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 战局模拟核：开战时由房间按会话角色创建，负责时钟与实体会话。
    /// </summary>
    public interface ISimulationKernel : ISubSystem
    {
        Simulator Simulator { get; }
        bool IsSessionRunning { get; }

        bool StartSession();
        void StopSession();
    }
}
