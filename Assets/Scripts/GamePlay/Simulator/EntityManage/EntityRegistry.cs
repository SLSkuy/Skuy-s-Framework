using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 模拟器实体注册器，统一管理需要进行模拟管理的所有实体
    /// </summary>
    public sealed class EntityRegistry
    {
        private readonly Dictionary<uint, RegisteredEntity> _entities = new();

        #region 属性
        public int Count => _entities.Count;
        public IReadOnlyDictionary<uint, RegisteredEntity> Entities => _entities;
        #endregion
        
        /// <summary>
        /// 托管注册实体
        /// </summary>
        public bool Register(EntityObjectIdentity identity, EntityCharacter character)
        {
            if (!identity || !character) return false;

            uint entityId = identity.EntityId;
            if (entityId == 0) return false;
            if (_entities.ContainsKey(entityId)) return false;

            _entities.Add(entityId, new RegisteredEntity(identity, character));
            return true;
        }

        /// <summary>
        /// 注销实体
        /// </summary>
        public bool Unregister(uint entityId)
        {
            return entityId != 0 && _entities.Remove(entityId);
        }

        public bool TryGet(uint entityId, out RegisteredEntity entity)
        {
            return _entities.TryGetValue(entityId, out entity);
        }

        public bool Contains(uint entityId)
        {
            return entityId != 0 && _entities.ContainsKey(entityId);
        }

        public void Clear()
        {
            _entities.Clear();
        }
    }
}
