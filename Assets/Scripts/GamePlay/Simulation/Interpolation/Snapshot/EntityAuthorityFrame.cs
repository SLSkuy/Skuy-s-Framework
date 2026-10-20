using System;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 可供远端角色插值使用的服务端快照
    /// </summary>
    [Serializable]
    public struct EntityAuthorityFrame
    {
        public uint snapshotTick;
        public EntitySnapshot state;
    }
}
