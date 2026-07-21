using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端远程玩家控制器，用于桥接与实体的连接，为实体传输快照源
    /// </summary>
    public class RemotePlayerController : AutoEventMonoBehaviour
    {
        private IPlayerCharacter _remotePlayerCharacter;

        /// <summary>
        /// 接收服务端下发的快照
        /// </summary>
        /// <param name="snapshot"></param>
        public void AddSnapshot(NetPlayerSnapshot snapshot)
        {
            
        }
    }
}
