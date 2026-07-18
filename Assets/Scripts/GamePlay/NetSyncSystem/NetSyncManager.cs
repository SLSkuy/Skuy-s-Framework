using Events;
using Framework;
using NetSync;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络同步控制器，控制主机/客户端的生命周期与消息同步
    /// </summary>
    public class NetSyncManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetSyncManager;
        
        // ========== 同步控制器 ==========
        private ClientSimulator _clientSimulator;
        private HostSimulator _hostSimulator;
        // ========== 同步控制器 ==========

        public void StartClient()
        {
            
        }

        public void StartHost()
        {
            
        }

        public void StopClient()
        {

        }

        public void StopHost()
        {
            
        }

        public void StopAll()
        {
            StopClient();
            StopHost();
        }

        #region Handler

        private void HandlePlayerInput(uint clientId, Player_Input input)
        {
            
        }

        private void HandlePlayerSnapshot(Player_Snapshot snapshot)
        {
            
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            
        }

        public override void Update(float deltaTime)
        {
            
        }

        public override void BindEvents()
        {
            Global.RegNetHandler<Player_Input>(NetEvent.PLAYER_INPUT, HandlePlayerInput);
            Global.RegNetHandler<Player_Snapshot>(NetEvent.PLAYER_SNAPSHOT, HandlePlayerSnapshot);
        }

        public override void Destroy()
        {
            
        }

        #endregion
    }
}