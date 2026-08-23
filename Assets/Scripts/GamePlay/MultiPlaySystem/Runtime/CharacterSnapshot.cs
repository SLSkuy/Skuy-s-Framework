using GamePlay.EntitySystem;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 可供远端角色插值使用的服务端快照。
    /// </summary>
    public struct CharacterSnapshot : IEntitySnapshot
    {
        public uint SnapshotTick { get; set; }
        public EntitySimulationState State;
    }
}
