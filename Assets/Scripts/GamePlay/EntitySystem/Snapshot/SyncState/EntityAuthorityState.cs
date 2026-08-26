using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 可供远端角色插值使用的服务端快照
    /// </summary>
    [Serializable]
    public struct EntityAuthorityState
    {
        public uint snapshotTick;
        public EntityRollbackState state;
    }
}
