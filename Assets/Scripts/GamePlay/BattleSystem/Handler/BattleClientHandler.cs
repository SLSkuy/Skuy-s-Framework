using System;
using System.Collections.Generic;
using Events;
using Framework;
using NetSync;
using Network;
using Utils;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 战斗客户端网络消息处理器
    /// </summary>
    public class BattleClientHandler : IHandler
    {
        private const int RECENT_INPUT_COUNT = 4;
        
        private readonly List<Input_Command> _recentInputs = new();
        private NetClient _client;

        #region 事件
        public event Action<World_Snapshot> WorldSnapshotReceived;
        public event Action<Player_Spawn_Notify> PlayerSpawnReceived;
        public event Action<Entity_Destroy_Notify> EntityDestroyReceived;
        #endregion

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client.RegisterHandler<Player_Spawn_Notify>(NetEvent.GAME_PLAYER_SPAWN_NOTIFY, HandlePlayerSpawnNotify);
            _client.RegisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
        }

        public void Unbind()
        {
            _recentInputs.Clear();
            if (_client == null) return;

            _client.UnregisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client.UnregisterHandler<Player_Spawn_Notify>(NetEvent.GAME_PLAYER_SPAWN_NOTIFY, HandlePlayerSpawnNotify);
            _client.UnregisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
            _client = null;
            
            WorldSnapshotReceived = null;
            PlayerSpawnReceived = null;
            EntityDestroyReceived = null;
        }

        #endregion
        
        #region 发送消息

        public void SendPlayerInput(uint inputTick, in InputCommand input)
        {
            Input_Command command = input.ToProto(inputTick);
            _recentInputs.Add(command);
            if (_recentInputs.Count > RECENT_INPUT_COUNT)
            {
                _recentInputs.RemoveAt(0);
            }

            Player_Inputs message = new Player_Inputs { CurrentInputs = command };
            foreach (Input_Command inputSample in _recentInputs)
            {
                message.RecentInputs.Add(inputSample);
            }

            _client.Send(NetEvent.GAME_PLAYER_INPUT, message);
        }
        
        public void SendSpawnSyncRequest()
        {
            _client.SendReliable(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, new Entity_Spawn_Sync_Request());
        }

        #endregion

        #region 接收消息
        
        private void HandleWorldSnapshot(World_Snapshot message)
        {
            WorldSnapshotReceived?.Invoke(message);
        }
        
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