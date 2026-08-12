namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体模拟状态的捕获与恢复边界。
    /// </summary>
    public interface IEntityStateStore
    {
        /// <summary>
        /// 捕获当前网络可见模拟状态。
        /// </summary>
        EntitySimulationState CaptureSimulationState();

        /// <summary>
        /// 捕获本地预测所需的完整回滚状态。
        /// </summary>
        EntityRollbackState CaptureRollbackState();

        /// <summary>
        /// 恢复完整回滚状态。
        /// </summary>
        void RestoreRollbackState(in EntityRollbackState state);
    }
}
