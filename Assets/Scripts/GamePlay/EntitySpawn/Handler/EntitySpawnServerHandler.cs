using Events;
using Framework;
using GamePlay.EntityFactory;
using GamePlay.Room;
using NetSync;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 实体生成的主机收发。只组包和解包，生成与注册在 EntitySpawner。
    /// </summary>
    public sealed class EntitySpawnServerHandler
    {
        private readonly EntitySpawner _spawner;
        private RoomManager _room;

        public EntitySpawnServerHandler(EntitySpawner spawner)
        {
            _spawner = spawner;
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
        public void BroadcastSpawnNotify(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            _room.BroadcastReliable(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, playerId, position, rotation));
        }

        /// <summary>
        /// 发送实体生成消息，用于给新加入的玩家
        /// </summary>
        public void SendSpawnNotify(uint playerId, uint entityId, EntityType entityTypeId, uint ownerPlayerId, Vector3 position, Quaternion rotation)
        {
            _room.SendReliable(playerId, NetEvent.GAME_ENTITY_SPAWN_NOTIFY,
                ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, ownerPlayerId, position, rotation));
        }

        public void BroadcastDestroy(uint entityId)
        {
            _room.BroadcastReliable(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, new Entity_Destroy_Notify { EntityId = entityId });
        }

        #endregion

        #region 接收消息

        /// <summary>
        /// 客户端首次连接时请求所有实体信息
        /// </summary>
        private void HandleEntitySpawnSyncRequest(uint playerId, Entity_Spawn_Sync_Request request)
        {
            _spawner.HandleSpawnSyncRequest(playerId);
        }

        #endregion
    }
}
