using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.Room;
using NetSync;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleServerHandler : IHandler
    {
        private RoomManager _room;

        
        #region 事件
        public event Action<Guid, Player_Inputs> PlayerInputReceived;
        public event Action<Guid> SpawnSyncRequested;
        #endregion

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _room = Global.Get<RoomManager>();
            _room.RegisterPlayerHandler<Player_Inputs>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room.RegisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
        }

        public void Unbind()
        {
            if (_room == null) return;

            _room.UnregisterPlayerHandler<Player_Inputs>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room.UnregisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
            _room = null;

            PlayerInputReceived = null;
            SpawnSyncRequested = null;
        }
        
        #endregion
        
        #region 发送消息
        
        public void BroadcastWorldSnapshot(uint snapshotTick, List<PlayerProcessedSnapshot> samples)
        {
            World_Snapshot message = new World_Snapshot { SnapshotTick = snapshotTick };
            foreach (var sample in samples)
            {
                message.PlayerSnapshots.Add(sample.snapshot.ToProto(sample.entityId, sample.lastProcessedInputTick));
            }

            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroup(activeIDs, NetEvent.GAME_WORLD_SNAPSHOT, message);
        }
        
        /// <summary>
        /// 广播实体生成消息，用于已经在房间中的玩家
        /// </summary>
        public void BroadcastPlayerSpawnNotify(uint entityId, Guid playerId, in EntitySpawnCommand command, in EntitySnapshot snapshot)
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroupReliable(activeIDs, NetEvent.GAME_PLAYER_SPAWN_NOTIFY, command.ToProto(entityId, playerId, snapshot));
        }

        /// <summary>
        /// 发送实体生成消息，用于给新加入的玩家
        /// </summary>
        public void SendPlayerSpawnNotify(Guid playerId, uint entityId, Guid ownerPlayerId, in EntitySpawnCommand command,
            in EntitySnapshot snapshot)
        {
            _room.SendReliable(playerId, NetEvent.GAME_PLAYER_SPAWN_NOTIFY, command.ToProto(entityId, ownerPlayerId, snapshot));
        }

        public void BroadcastEntityDestroyNotify(in EntityDestroyCommand command)
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroupReliable(activeIDs, NetEvent.GAME_ENTITY_DESTROY_NOTIFY, command.ToProto());
        }


        #endregion

        #region 接收消息
        
        private void HandlePlayerInput(Guid playerId, Player_Inputs message)
        {
            PlayerInputReceived?.Invoke(playerId, message);
        }
        
                
        /// <summary>
        /// 客户端首次连接时请求所有实体信息
        /// </summary>
        private void HandleEntitySpawnSyncRequest(Guid playerId, Entity_Spawn_Sync_Request request)
        {
            SpawnSyncRequested?.Invoke(playerId);
        }

        #endregion
    }
}