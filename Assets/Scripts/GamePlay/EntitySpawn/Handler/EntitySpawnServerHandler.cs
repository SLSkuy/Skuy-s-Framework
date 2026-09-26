using Events;
using Framework;
using GamePlay.EntityFactory;
using NetSync;
using Network;
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
        private NetServer _server;

        public EntitySpawnServerHandler(EntitySpawner spawner)
        {
            _spawner = spawner;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _server = Global.Get<NetServer>();
            _server.RegisterHandler<Entity_Spawn_Sync_Request>(NetEvent.ENTITY_SPAWN_SYNC_REQUEST, HandleSpawnSyncRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;

            _server.UnregisterHandler<Entity_Spawn_Sync_Request>(NetEvent.ENTITY_SPAWN_SYNC_REQUEST, HandleSpawnSyncRequest);
            _server = null;
        }

        #endregion

        #region 发送消息

        public void BroadcastSpawn(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            _server.BroadcastReliable(NetEvent.ENTITY_SPAWN_NOTIFY, ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, playerId, position, rotation));
        }

        public void SendSpawn(uint connectionId, uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            _server.SendReliable(connectionId, NetEvent.ENTITY_SPAWN_NOTIFY, ProtoUtils.ToEntitySpawnNotify(entityId, entityTypeId, playerId, position, rotation));
        }

        public void BroadcastDestroy(uint entityId)
        {
            _server.BroadcastReliable(NetEvent.ENTITY_DESTROY_NOTIFY, new Entity_Destroy_Notify { EntityId = entityId });
        }

        #endregion

        #region 接收消息

        private void HandleSpawnSyncRequest(uint connectionId, Entity_Spawn_Sync_Request request)
        {
            _spawner.HandleSpawnSyncRequest(connectionId);
        }

        #endregion
    }
}
