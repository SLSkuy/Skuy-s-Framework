using System;
using Events;
using Framework;
using GamePlay.EntitySpawn;
using GamePlay.EntitySystem;
using GamePlay.Room;
using NetSync;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleEntityServerHandler
    {
        private readonly BattleEntityManager _entityManager;
        private RoomManager _room;

        public BattleEntityServerHandler(BattleEntityManager entityManager)
        {
            _entityManager = entityManager;
        }
        
        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _room = Global.Get<RoomManager>();
            _room.RegisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
        }

        public void Unbind()
        {
            if (_room == null) return;
            
            _room.UnregisterPlayerHandler<Entity_Spawn_Sync_Request>(NetEvent.GAME_ENTITY_SPAWN_SYNC_REQUEST, HandleEntitySpawnSyncRequest);
            _room = null;
        }
        
        #endregion
        
        #region 发送消息
        
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
        
        /// <summary>
        /// 客户端首次连接时请求所有实体信息
        /// </summary>
        private void HandleEntitySpawnSyncRequest(Guid playerId, Entity_Spawn_Sync_Request request)
        {
            _entityManager.HandleSpawnSyncRequest(playerId);
        }

        #endregion
    }
}