using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟核中模拟实体对象管理
    /// </summary>
    public class SimulationEntityManager : IDisposable
    {
        private readonly Dictionary<uint, SimulationEntity> _entities = new();

        #region 属性
        public int Count => _entities.Count;
        public IReadOnlyDictionary<uint, SimulationEntity> Entities => _entities;
        #endregion
        
        /// <summary>
        /// 托管注册实体
        /// </summary>
        public bool Register(EntityIdentity identity, EntityCharacter character)
        {
            if (!identity || !character) return false;

            uint entityId = identity.EntityId;
            if (entityId == 0) return false;
            if (_entities.ContainsKey(entityId)) return false;

            _entities.Add(entityId, new SimulationEntity(identity, character));
            return true;
        }

        /// <summary>
        /// 注销实体
        /// </summary>
        public bool Unregister(uint entityId)
        {
            return entityId != 0 && _entities.Remove(entityId);
        }

        public bool TryGet(uint entityId, out SimulationEntity entity)
        {
            return _entities.TryGetValue(entityId, out entity);
        }

        public bool Contains(uint entityId)
        {
            return entityId != 0 && _entities.ContainsKey(entityId);
        }
        
        public void Dispose()
        {
            _entities.Clear();
        }
    }
}
