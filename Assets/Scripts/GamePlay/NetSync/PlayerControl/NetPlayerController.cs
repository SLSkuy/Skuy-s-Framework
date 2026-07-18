using Framework;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 远程玩家模拟控制器，
    /// </summary>
    public class NetPlayerController : IPlayerController
    {
        public uint ClientId { get; }
        public IInputProvider InputProvider { get; }
        public InputState LastSendInput { get; }
        public NetPlayerEntity Entity { get; }
    }
}