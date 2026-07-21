using UnityEngine;

namespace GamePlay.EntitySystem
{
    public interface IEntitySnapshot
    {
        /// <summary>
        /// 快照所属的网络实体。
        /// </summary>
        uint EntityId { get; set; }

        /// <summary>
        /// 服务器Tick
        /// </summary>
        uint SnapshotTick { get; set; }

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}
