using System;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Simulation;
using UnityEngine;
using YooAsset;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 对局实体的实例化与实体号。主机分配 EntityId，客户端写入通知中的实体号。
    /// </summary>
    public class EntityFactory : IDisposable
    {
        private readonly EntitySpawnConfigProxy _spawnConfigData = Global.GetDataProxy<EntitySpawnConfigProxy>();
        private EntityIdAllocator _allocator = new();

        /// <summary>
        /// 同步生成本地实体
        /// </summary>
        public GameObject SpawnEntity(EntityType type, Vector3 position, Quaternion rotation)
        {
            string location = _spawnConfigData.GetEntityLocation(type);
            InstantiateOptions options = new (true, position, rotation);
            GameObject instance = Global.Instantiate(location, options);
            if (instance.TryGetComponent(out EntityIdentity entityIdentity))
            {
                throw new InvalidOperationException($"实例 {instance.name} 存在 {nameof(EntityIdentity)}，需使用SpawnNetEntity函数进行网络实体生成");
            }
            return instance;
        }

        /// <summary>
        /// 同步生成网络实体
        /// </summary>
        public EntityIdentity SpawnNetEntity(EntityType type)
        {
            EntitySpawnCommand command = new EntitySpawnCommand(type);
            return SpawnNetEntity(command);
        }

        /// <summary>
        /// 同步生成网络实体
        /// </summary>
        public EntityIdentity SpawnNetEntity(EntitySpawnCommand command)
        {
            GameObject instance = InstantiateEntity(command);
            if (instance.TryGetComponent(out EntityIdentity entityIdentity))
            {
                _allocator.AllocateEntityId(entityIdentity);
                entityIdentity.BindEntityType(command.entityTypeId);
                return entityIdentity;
            }

            throw new InvalidOperationException($"实例 {instance.name} 缺少 {nameof(EntityIdentity)}，不能分配实体号");
        }

        /// <summary>
        /// 同步销毁实体
        /// </summary>
        public void DestroyEntity(uint entityId)
        {
            if (_allocator.RemoveEntity(entityId, out EntityIdentity identity))
            {
                Global.Release(identity.gameObject);
            }
        }

        /// <summary>
        /// 同步清掉本局已生成的实体。须在模拟核销毁之前调用。
        /// </summary>
        public void DestroyAll()
        {
            foreach (var record in _allocator.GetActiveEntities())
            {
                DestroyEntity(record.EntityId);
            }
        }

        /// <summary>
        /// 按实体号查找本局已生成的网络实体。重复的生成通知会找到已有实例。
        /// </summary>
        public bool TryGetEntity(uint entityId, out EntityIdentity identity)
        {
            return _allocator.TryGetEntity(entityId, out identity);
        }

        private GameObject InstantiateEntity(in EntitySpawnCommand command)
        {
            string location = _spawnConfigData.GetEntityLocation(command.entityTypeId);
            InstantiateOptions options = new (true, command.position, command.rotation);
            return Global.Instantiate(location, options);
        }

        #region 子系统生命周期

        public void Dispose()
        {
            _allocator.Dispose();
            _allocator = null;
        }

        #endregion

        #region 网络消息处理

        public EntityIdentity HandleSpawnEntityCommand(uint entityId, in EntitySpawnCommand command)
        {
            GameObject instance = InstantiateEntity(command);
            if (instance.TryGetComponent(out EntityIdentity entityIdentity))
            {
                _allocator.AddEntity(entityId, entityIdentity);
                entityIdentity.BindEntityType(command.entityTypeId);
                return entityIdentity;
            }

            throw new InvalidOperationException($"实例 {instance.name} 缺少 {nameof(EntityIdentity)}，不能分配实体号");
        }

        #endregion
    }
}
