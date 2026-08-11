namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 可由同步根组件驱动的同步组件。
    /// </summary>
    public interface INetSyncUpdatable
    {
        /// <summary>
        /// 同步帧更新。
        /// </summary>
        void SyncUpdate(float deltaTime);
    }
}
