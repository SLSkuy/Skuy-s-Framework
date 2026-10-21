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
        private const int RECENT_INPUT_COUNT = 8;
        
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

        public void SendPlayerInput(uint inputTick, in InputState input)
        {
            Input_Sample sample = ProtoUtils.ToInputSample(inputTick, input);
            _recentInputs.Add(sample);
            if (_recentInputs.Count > RECENT_INPUT_COUNT)
            {
                _recentInputs.RemoveAt(0);
            }

            Player_Input message = input.ToProto(inputTick);
            for (int i = 0; i < _recentInputs.Count; i++)
            {
                message.RecentInputs.Add(_recentInputs[i]);
            }

            _client.Send(NetEvent.GAME_PLAYER_INPUT, message);
        }

        #endregion

        #region 接收消息
        
        private void HandleWorldSnapshot(World_Snapshot message)
        {
            _battleManager.HandleWorldSnapshot(message);
        }

        private void HandleEntitySpawnNotify(Entity_Spawn_Notify message)
        {
            // _factory.HandleSpawnNotify(message.EntityId, (EntityType)message.EntityTypeId, message.PlayerId,
                // ProtoUtils.ToUnity(message.Position), ProtoUtils.ToUnity(message.Rotation));
        }

        private void HandleEntityDestroyNotify(Entity_Destroy_Notify message)
        {
            _battleManager.HandleDestroyNotify(message.EntityId);
        }

        #endregion
    }
}