using System;
using System.Collections.Generic;
using System.Numerics;
using GamePlay.EntitySpawn;
using GamePlay.Simulation;
using NetSync;
using Network;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleServerHandler
    {
        private BattleManager _battleManager;
        private NetServer _server;

        public BattleServerHandler(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }
        
        #region 消息绑定

        public void Bind()
        {
            Unbind();

            // _room.RegisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            // _room.RegisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
        }

        public void Unbind()
        {
            // _room.UnregisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            // _room.UnregisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
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

            // _room.Broadcast(NetEvent.GAME_WORLD_SNAPSHOT, message);
        }

        /// <summary>
        /// 广播实体生成消息，用于已经在房间中的玩家
        /// </summary>
        public void BroadcastSpawnNotify(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            // _room.BroadcastReliable(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, playerId, position, rotation));
        }

        /// <summary>
        /// 发送实体生成消息，用于给新加入的玩家
        /// </summary>
        public void SendSpawnNotify(uint playerId, uint entityId, EntityType entityTypeId, uint ownerPlayerId, Vector3 position, Quaternion rotation)
        {
            // _room.SendReliable(playerId, NetEvent.GAME_ENTITY_SPAWN_NOTIFY, ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, ownerPlayerId, position, rotation));
        }

        public void BroadcastDestroy(uint entityId)
        {
            // _room.BroadcastReliable(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, new Entity_Destroy_Notify { EntityId = entityId });
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