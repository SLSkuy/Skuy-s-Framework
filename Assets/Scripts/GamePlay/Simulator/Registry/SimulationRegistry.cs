using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 按非零 entityId 登记可模拟实体，拒绝重复标识。
    /// </summary>
    public sealed class SimulationRegistry
    {
        private readonly Dictionary<uint, RegisteredEntity> _entities = new();

        #region 属性
        public int Count => _entities.Count;
        public IReadOnlyDictionary<uint, RegisteredEntity> Entities => _entities;
        #endregion

        public readonly struct RegisteredEntity
        {
            public RegisteredEntity(EntityObjectIdentity identity, EntityCharacter character)
            {
                Identity = identity;
                Character = character;
            }

            #region 属性
            public EntityObjectIdentity Identity { get; }
            public EntityCharacter Character { get; }
            #endregion
        }

        public bool Register(EntityObjectIdentity identity, EntityCharacter character)
        {
            if (identity == null || character == null) return false;

            uint entityId = identity.EntityId;
            if (entityId == 0) return false;
            if (_entities.ContainsKey(entityId)) return false;

            _entities.Add(entityId, new RegisteredEntity(identity, character));
            return true;
        }

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
