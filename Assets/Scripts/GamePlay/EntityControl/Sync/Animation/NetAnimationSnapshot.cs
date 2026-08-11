namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 动画同步快照。
    /// </summary>
    public struct NetAnimationSnapshot : IEntitySnapshot
    {
        public uint EntityId { get; set; }
        public uint SnapshotTick { get; set; }
        public int StateHash { get; set; }
        public float NormalizedTime { get; set; }
        public float LocomotionSpeed { get; set; }
    }
}
