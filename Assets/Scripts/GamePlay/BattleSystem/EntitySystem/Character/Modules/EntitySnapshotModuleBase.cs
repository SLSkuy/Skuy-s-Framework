namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 支持捕获与恢复回滚快照的实体能力模块。
    /// </summary>
    public abstract class EntitySnapshotModuleBase<TState> : EntityModuleBase
    {
        public abstract TState CaptureSnapshot();
        public abstract void RestoreSnapshot(in TState state);
    }
}
