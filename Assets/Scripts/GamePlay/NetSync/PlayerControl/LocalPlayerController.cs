using Framework;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 本地玩家控制器
    /// </summary>
    public class LocalPlayerController : IPlayerController
    {
        public uint ClientId { get; }
        public IInputProvider InputProvider { get; }
        public InputState LastSendInput { get; }
    }
}