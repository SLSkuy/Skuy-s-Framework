namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端回放同步入口。
    /// </summary>
    public interface INetReplayModule
    {
        /// <summary>
        /// 回放未确认输入。
        /// </summary>
        void Replay(uint fromInputTick, uint toInputTick, float tickDeltaTime);
    }
}
