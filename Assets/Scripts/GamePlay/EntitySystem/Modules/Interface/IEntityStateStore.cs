namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态缓存接口
    /// </summary>
    public interface IEntityStateStore<T> where T : struct
    {
        /// <summary>
        /// 捕获当前实体完整状态
        /// </summary>
        T CaptureSnapshot();
        
        /// <summary>
        /// 恢复当前实体完整状态
        /// </summary>
        void RestoreSnapshot(in T state);
    }
}
