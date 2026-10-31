using System;
using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.BattleSystem
{
    public class BattleEntityClientHandler : IHandler
    {
        private NetClient _client;

        #region 事件
        public event Action<Player_Spawn_Notify> PlayerSpawnReceived;
        public event Action<Entity_Destroy_Notify> EntityDestroyReceived;
        #endregion

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<Player_Spawn_Notify>(NetEvent.GAME_PLAYER_SPAWN_NOTIFY, HandlePlayerSpawnNotify);
            _client.RegisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;
            
            _client.UnregisterHandler<Player_Spawn_Notify>(NetEvent.GAME_PLAYER_SPAWN_NOTIFY, HandlePlayerSpawnNotify);
            _client.UnregisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
            _client = null;

            PlayerSpawnReceived = null;
            EntityDestroyReceived = null;
        }

        #endregion

        #region 发送消息

        public void SendSpawnSyncRequest()
        {
            _client.SendReliable(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, new Entity_Spawn_Sync_Request());
        }

        #endregion
        
        #region 接收消息
        
        private void HandlePlayerSpawnNotify(Player_Spawn_Notify notify)
        {
            PlayerSpawnReceived?.Invoke(notify);
        }

        private void HandleEntityDestroyNotify(Entity_Destroy_Notify notify)
        {
            EntityDestroyReceived?.Invoke(notify);
        }
        
        #endregion
    }
}