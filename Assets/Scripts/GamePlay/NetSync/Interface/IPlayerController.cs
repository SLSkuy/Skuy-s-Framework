using Framework;

namespace GamePlay.NetSync
{
    public interface IPlayerController
    {
        /// <summary>
        /// 控制对象所属客户端ID
        /// </summary>
        uint ClientId { get; }
        
        IInputProvider InputProvider { get; }
        InputState LastSendInput { get; }
    }
}