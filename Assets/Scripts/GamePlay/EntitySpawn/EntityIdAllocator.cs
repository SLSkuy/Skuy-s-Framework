using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay.Simulation;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 主机实体号分配。从 1 递增，本局不回收。同时保留存活实体，供后加入补发。
    /// </summary>
    public class EntityIdAllocator : IDisposable
    {
        private readonly Dictionary<uint, EntityIdentity> _entityIdentity = new();
        private uint _nextEntityId = 1;

        /// <summary>
        /// 分配实体ID
        /// </summary>
        public uint AllocateEntityId(EntityIdentity identity)
        {
            identity.BindEntityID(_nextEntityId++);
            _entityIdentity.Add(identity.EntityId, identity);
            return identity.EntityId;
        }
        
        public bool TryGetEntity(uint entityId, out EntityIdentity identity)
        {
            return _entityIdentity.TryGetValue(entityId, out identity);
        }

        public bool RemoveEntity(uint entityId, out EntityIdentity identity)
        {
            return _entityIdentity.Remove(entityId, out identity);
        }

        /// <summary>
        /// 获取当前存活的所有实体数据
        /// </summary>
        /// <returns></returns>
        public List<EntityIdentity> GetActiveEntities()
        {
            return _entityIdentity.Values.ToList();
        }

        public void Dispose()
        {
            _entityIdentity.Clear();
            _nextEntityId = 1;
        }
    }
}
