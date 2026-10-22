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
    public class BattleClientHandler
    {
        private const int RECENT_INPUT_COUNT = 4;
        
        private readonly List<Input_Sample> _recentInputs = new();
        private readonly BattleManager _battleManager;
        private NetClient _client;

        public BattleClientHandler(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client.RegisterHandler<Entity_Spawn_Notify>(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, HandleEntitySpawnNotify);
            _client.RegisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
        }

        public void Unbind()
        {
            _recentInputs.Clear();
            if (_client == null) return;

            _client.UnregisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client.UnregisterHandler<Entity_Spawn_Notify>(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, HandleEntitySpawnNotify);
            _client.UnregisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
            _client = null;
        }

        #endregion
        
        #region 发送消息

        public void SendSpawnSyncRequest()
        {
            _client.SendReliable(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, new Entity_Spawn_Sync_Request());
        }

        public void SendPlayerInput(uint inputTick, in InputState input)
        {
            Input_Sample sample = ProtoUtils.ToInputSample(inputTick, input);
            _recentInputs.Add(sample);
            if (_recentInputs.Count > RECENT_INPUT_COUNT)
            {
                _recentInputs.RemoveAt(0);
            }

            Player_Input message = input.ToProto(inputTick);
            foreach (var inputSample in _recentInputs)
            {
                message.RecentInputs.Add(inputSample);
            }

            _client.Send(NetEvent.GAME_PLAYER_INPUT, message);
        }

        #endregion

        #region 接收消息
        
        private void HandleWorldSnapshot(World_Snapshot message)
        {
            _battleManager.HandleWorldSnapshot(message);
        }

        private void HandleEntitySpawnNotify(Entity_Spawn_Notify notify)
        {
            _battleManager.HandleSpawnNotify(notify.EntityId, Guid.Parse(notify.PlayerId), notify.ToCommand());
        }

        private void HandleEntityDestroyNotify(Entity_Destroy_Notify notify)
        {
            _battleManager.HandleDestroyNotify(notify.ToCommand());
        }

        #endregion
    }
}