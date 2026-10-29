using System;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 可供远端角色插值使用的服务端快照
    /// </summary>
    [Serializable]
    public struct EntityAuthoritySnapshot
    {
        public uint snapshotTick;
        public EntitySnapshot state;
    }
}
