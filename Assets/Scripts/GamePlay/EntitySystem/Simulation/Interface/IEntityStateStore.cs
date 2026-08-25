namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体状态缓存接口
    /// </summary>
    public interface IEntityStateStore
    {
        /// <summary>
        /// 捕获当前实体基础状态（状态机）
        /// </summary>
        EntitySimulationState CaptureSimulationState();
        
        /// <summary>
        /// 捕获当前实体完整状态
        /// </summary>
        EntityRollbackState CaptureRollbackState();
        
        /// <summary>
        /// 恢复当前实体完整状态
        /// </summary>
        void RestoreRollbackState(in EntityRollbackState state);
    }
}
