namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 技能同步快照。
    /// </summary>
    public struct NetSkillSnapshot : IEntitySnapshot
    {
        public uint EntityId { get; set; }
        public uint SnapshotTick { get; set; }
        public uint SkillId { get; set; }
        public uint SkillState { get; set; }
        public float NormalizedTime { get; set; }
    }
}
