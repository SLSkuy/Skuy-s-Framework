using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 位移同步快照
    /// </summary>
    public struct NetTransformSnapshot : IEntitySnapshot
    {
        public uint EntityId { get; set; }
        public uint SnapshotTick { get; set; }
        public uint LastProcessedInputTick { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public uint MovementState { get; set; }
    }
}
