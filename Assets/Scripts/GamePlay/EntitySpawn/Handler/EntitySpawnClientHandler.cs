using Events;
using Framework;
using GamePlay.EntityFactory;
using NetSync;
using Network;
using Utils;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 实体生成的客户端收发。关卡就绪后请求主机补发已有实体。
    /// </summary>
    public sealed class EntitySpawnClientHandler
    {
        private readonly EntitySpawner _spawner;
        private NetClient _client;

        public EntitySpawnClientHandler(EntitySpawner spawner)
        {
            _spawner = spawner;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<Entity_Spawn_Notify>(NetEvent.ENTITY_SPAWN_NOTIFY, HandleSpawnNotify);
            _client.RegisterHandler<Entity_Destroy_Notify>(NetEvent.ENTITY_DESTROY_NOTIFY, HandleDestroyNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.UnregisterHandler<Entity_Spawn_Notify>(NetEvent.ENTITY_SPAWN_NOTIFY, HandleSpawnNotify);
            _client.UnregisterHandler<Entity_Destroy_Notify>(NetEvent.ENTITY_DESTROY_NOTIFY, HandleDestroyNotify);
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendSpawnSyncRequest()
        {
            _client.SendReliable(NetEvent.ENTITY_SPAWN_SYNC_REQUEST, new Entity_Spawn_Sync_Request());
        }

        #endregion

        #region 接收消息

        private void HandleSpawnNotify(Entity_Spawn_Notify message)
        {
            _spawner.HandleSpawnNotify(message.EntityId, (EntityType)message.EntityTypeId, message.PlayerId,
                ProtoUtils.ToUnity(message.Position), ProtoUtils.ToUnity(message.Rotation));
        }

        private void HandleDestroyNotify(Entity_Destroy_Notify message)
        {
            _spawner.HandleDestroyNotify(message.EntityId);
        }

        #endregion
    }
}
