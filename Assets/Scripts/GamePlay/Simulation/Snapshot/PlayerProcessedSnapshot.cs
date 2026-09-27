using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家在某一Tick上模拟处理完成后的快照状态
    /// </summary>
    public struct PlayerProcessedSnapshot
    {
        public uint entityId;
        public uint playerId;
        public uint lastProcessedInputTick;
        public EntityRollbackState state;
    }
}
