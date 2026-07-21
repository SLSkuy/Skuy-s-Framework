using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络玩家实体快照
    /// </summary>
    public struct NetPlayerSnapshot : IPlayerSnapshot
    {
        public uint EntityId { get; set; }
        public uint SnapshotTick { get; set; }
        public uint LastProcessedInputTick { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}
