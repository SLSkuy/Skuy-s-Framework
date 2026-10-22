using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySpawn;
using GamePlay.Room;
using GamePlay.Simulation;
using NetSync;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleServerHandler
    {
        private readonly BattleManager _battleManager;
        private RoomManager _room;

        public BattleServerHandler(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }
        
        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _room = Global.Get<RoomManager>();
            _room.RegisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room.RegisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
        }

        public void Unbind()
        {
            if (_room == null) return;

            _room.UnregisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room.UnregisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
            _room = null;
        }
        
        #endregion
        
        #region 发送消息
        
        public void BroadcastWorldSnapshot(uint snapshotTick, List<PlayerProcessedFrame> samples)
        {
            World_Snapshot message = new World_Snapshot
            {
                SnapshotTick = snapshotTick,
            };
            foreach (var sample in samples)
            {
                message.PlayerSnapshots.Add(ProtoUtils.ToPlayerSnapshot(sample.entityId, sample.lastProcessedInputTick, sample.snapshot));
            }

            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroup(activeIDs, NetEvent.GAME_WORLD_SNAPSHOT, message);
        }

        /// <summary>
        /// 广播实体生成消息，用于已经在房间中的玩家
        /// </summary>
        public void BroadcastSpawnNotify(uint entityId, Guid playerId, in EntitySpawnCommand command)
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroupReliable(activeIDs, NetEvent.GAME_ENTITY_SPAWN_NOTIFY, command.ToProto(entityId, playerId));
        }

        /// <summary>
        /// 发送实体生成消息，用于给新加入的玩家
        /// </summary>
        public void SendSpawnNotify(Guid playerId, uint entityId, Guid ownerPlayerId, in EntitySpawnCommand command)
        {
            _room.SendReliable(playerId, NetEvent.GAME_ENTITY_SPAWN_NOTIFY, command.ToProto(entityId, ownerPlayerId));
        }

        public void BroadcastDestroyNotify(in EntityDestroyCommand command)
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroupReliable(activeIDs, NetEvent.GAME_ENTITY_DESTROY_NOTIFY, command.ToProto());
        }

        #endregion

        #region 接收消息
        
        private void HandlePlayerInput(Guid playerId, Player_Input message)
        {
            _battleManager.HandlePlayerInput(playerId, message);
        }

        /// <summary>
        /// 客户端首次连接时请求所有实体信息
        /// </summary>
        private void HandleEntitySpawnSyncRequest(Guid playerId, Entity_Spawn_Sync_Request request)
        {
            _battleManager.HandleSpawnSyncRequest(playerId);
        }

        #endregion
    }
}