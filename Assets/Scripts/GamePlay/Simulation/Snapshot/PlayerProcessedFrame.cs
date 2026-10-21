using System;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家在某一Tick上模拟处理完成后的Tick帧状态
    /// </summary>
    public struct PlayerProcessedFrame
    {
        public uint entityId;
        public Guid playerId;
        public uint lastProcessedInputTick;
        public EntitySnapshot snapshot;
    }
}
