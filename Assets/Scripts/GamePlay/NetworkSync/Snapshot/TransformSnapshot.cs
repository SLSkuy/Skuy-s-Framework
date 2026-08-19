using UnityEngine;
using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 可供远端 Transform 插值使用的服务端快照。
    /// </summary>
    public struct TransformSnapshot : IEntitySnapshot
    {
        public uint SnapshotTick { get; set; }
        public EntitySimulationState State;
    }
}
