using System.Collections.Generic;
using GamePlay.EntityFactory;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 主机实体号分配。从 1 递增，本局不回收。同时保留存活实体，供后加入补发。
    /// </summary>
    public sealed class EntityIdAllocator
    {
        public readonly struct EntityIdRecord
        {
            public readonly uint EntityId;
            public readonly EntityType EntityTypeId;

            public EntityIdRecord(uint entityId, EntityType entityTypeId)
            {
                EntityId = entityId;
                EntityTypeId = entityTypeId;
            }
        }

        private uint _nextId = 1;
        private readonly Dictionary<uint, EntityIdRecord> _live = new();
        private readonly Dictionary<uint, uint> _entityByPlayer = new();
        private readonly Dictionary<uint, uint> _playerByEntity = new();

        /// <summary>
        /// 分配玩家实体对应的实体ID
        /// </summary>
        public uint AllocateByPlayer(uint playerId, EntityType entityTypeId)
        {
            uint entityId = _nextId;
            _nextId++;

            EntityIdRecord record = new(entityId, entityTypeId);
            _live.Add(entityId, record);
            BindPlayer(playerId, entityId);
            return entityId;
        }

        /// <summary>
        /// 记下主机已经分配好的玩家实体。客户端不另行分配。
        /// </summary>
        public void AdoptByPlayer(uint playerId, uint entityId, EntityType entityTypeId)
        {
            EntityIdRecord record = new(entityId, entityTypeId);
            _live.Add(entityId, record);
            BindPlayer(playerId, entityId);
            if (entityId >= _nextId) _nextId = entityId + 1;
        }

        /// <summary>
        /// 分配非玩家实体ID
        /// </summary>
        public uint Allocate(EntityType entityTypeId)
        {
            uint entityId = _nextId;
            _nextId++;

            EntityIdRecord record = new(entityId, entityTypeId);
            _live.Add(entityId, record);
            return entityId;
        }

        /// <summary>
        /// 通过PlayerID获取EntityID
        /// </summary>
        public uint GetByPlayer(uint playerId)
        {
            return _entityByPlayer[playerId];
        }

        /// <summary>
        /// 通过实体号取主人玩家号。非玩家实体返回 0。
        /// </summary>
        public uint GetPlayerId(uint entityId)
        {
            return _playerByEntity.TryGetValue(entityId, out uint playerId) ? playerId : 0;
        }

        public void RemoveByPlayer(uint playerId)
        {
            uint entityId = _entityByPlayer[playerId];
            UnbindPlayer(entityId);
            _live.Remove(entityId);
        }

        /// <summary>
        /// 从存活表移除，并清掉玩家映射。
        /// </summary>
        public void Remove(uint entityId)
        {
            UnbindPlayer(entityId);
            _live.Remove(entityId);
        }

        /// <summary>
        /// 获取所有存活实体列表，用于全量同步
        /// </summary>
        public EntityIdRecord[] GetLiveEntityIds()
        {
            EntityIdRecord[] records = new EntityIdRecord[_live.Count];
            _live.Values.CopyTo(records, 0);
            return records;
        }

        #region 名册映射处理

        private void BindPlayer(uint playerId, uint entityId)
        {
            _entityByPlayer.Add(playerId, entityId);
            _playerByEntity.Add(entityId, playerId);
        }

        private void UnbindPlayer(uint entityId)
        {
            if (!_playerByEntity.Remove(entityId, out uint playerId)) return;

            _entityByPlayer.Remove(playerId);
        }

        #endregion
    }
}
