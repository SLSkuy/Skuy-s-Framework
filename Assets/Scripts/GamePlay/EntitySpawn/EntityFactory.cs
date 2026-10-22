using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Simulation;
using UnityEngine;
using YooAsset;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 对局实体生成。主机分配 EntityId 并注册进模拟核；开听后把生成结果发给客户端。客户端只消费生成通知。
    /// </summary>
    public class EntityFactory : IDisposable
    {
        private readonly EntitySpawnConfigProxy _spawnConfigData = Global.GetDataProxy<EntitySpawnConfigProxy>();
        
        private readonly Queue<IEntityFactoryCommand> _commandQueue = new();
        private EntityIdAllocator _allocator = new();

        #region 事件
        public event Action<EntityType, GameObject> OnEntitySpawn;
        public event Action<EntityType, EntityIdentity> OnNetEntitySpawn;
        #endregion

        /// <summary>
        /// 请求实体生成，实体将以每帧固定数量分帧生成
        /// </summary>
        public void RequireSpawnEntity(EntityType type)
        {
            RequireSpawnEntity(type, Vector3.zero, Quaternion.identity);
        }
        
        /// <summary>
        /// 请求实体生成，实体将以每帧固定数量分帧生成
        /// </summary>
        public void RequireSpawnEntity(EntityType type, Vector3 position, Quaternion rotation)
        {
            EntitySpawnCommand command = new EntitySpawnCommand(type, position, rotation);
            EnqueueCommand(command);
        }

        /// <summary>
        /// 入队请求销毁实体
        /// </summary>
        /// <param name="entityId"></param>
        public void RequireDestroyEntity(uint entityId)
        {
            EntityDestroyCommand command = new EntityDestroyCommand(entityId);
            EnqueueCommand(command);
        }

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
            return SpawnNetEntity(type, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 同步生成网络实体
        /// </summary>
        public EntityIdentity SpawnNetEntity(EntityType type, Vector3 position, Quaternion rotation)
        {
            string location = _spawnConfigData.GetEntityLocation(type);
            InstantiateOptions options = new (true, position, rotation);
            GameObject instance = Global.Instantiate(location, options);
            if (instance.TryGetComponent(out EntityIdentity entityIdentity))
            {
                _allocator.AllocateEntityId(entityIdentity);
                return entityIdentity;
            }
            
            throw new InvalidOperationException($"实例 {instance.name} 缺少 {nameof(EntityIdentity)}，不能分配实体号"); 
        }

        /// <summary>
        /// 同步销毁实体
        /// </summary>
        /// <param name="entityId"></param>
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

        public List<EntityIdentity> GetActiveEntities()
        {
            return _allocator.GetActiveEntities();
        }

        #region 命令执行
        
        private void EnqueueCommand(IEntityFactoryCommand command)
        {
            _commandQueue.Enqueue(command);
        }

        private void ExecuteCommand()
        {
            int executeTime = 0;
            while (_commandQueue.Count != 0 && executeTime < _spawnConfigData.MaxExecutePerFrame)
            {
                DoExecute(_commandQueue.Dequeue());
                executeTime++;
            }
        }

        private void DoExecute(IEntityFactoryCommand command)
        {
            switch (command)
            {
                case EntitySpawnCommand spawnCommand:
                {
                    // TODO: 完善实体ID分配逻辑
                    string location = _spawnConfigData.GetEntityLocation(spawnCommand.entityTypeId);
                    InstantiateOptions options = new (true, spawnCommand.position, spawnCommand.rotation);
                    GameObject instance = Global.Instantiate(location, options);
                    if (instance.TryGetComponent(out EntityIdentity entityIdentity))
                    {
                        // 网络实体生成
                        _allocator.AllocateEntityId(entityIdentity);
                        OnNetEntitySpawn?.Invoke(spawnCommand.entityTypeId, entityIdentity);
                    }
                    else
                    {
                        // 本地实体生成
                        OnEntitySpawn?.Invoke(spawnCommand.entityTypeId, instance);
                    }
                    break;
                }
                case EntityDestroyCommand destroyCommand:
                    if (_allocator.RemoveEntity(destroyCommand.entityId, out EntityIdentity identity))
                    {
                        Global.Release(identity.gameObject);
                    }
                    break;
            }
        }

        #endregion
        
        #region 子系统生命周期
        
        public void Update()
        {
            ExecuteCommand();
        }
        
        public void Dispose()
        {
            _allocator.Dispose();
            _allocator = null;
        }

        #endregion
    }
}
