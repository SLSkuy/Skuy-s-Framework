using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 模拟核：由对局流程态按会话角色创建，负责时钟与实体会话。
    /// </summary>
    public interface ISimulationKernel : IDisposable
    {
        bool IsSessionRunning { get; }
        Simulator Kernel { get; }

        bool StartSession();
        void StopSession();
        void Update(float deltaTime);
    }
}
