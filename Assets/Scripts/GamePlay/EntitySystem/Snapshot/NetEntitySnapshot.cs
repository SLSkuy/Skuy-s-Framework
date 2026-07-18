using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体快照
    /// </summary>
    public struct NetEntitySnapshot : IEntitySnapshot
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}