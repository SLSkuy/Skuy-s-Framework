using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络玩家实体快照
    /// </summary>
    public struct NetPlayerSnapshot : IPlayerSnapshot
    {
        // TODO: 暂时只保留最小状态同步
        public uint ClientId { get; set; }
        public uint SnapshotTick { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}
