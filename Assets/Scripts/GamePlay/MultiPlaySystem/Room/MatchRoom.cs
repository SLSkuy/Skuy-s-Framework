using System.Collections.Generic;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 一场一房：连接标识到实体与所有者的映射。
    /// </summary>
    public sealed class MatchRoom
    {
        private readonly Dictionary<uint, uint> _entityByClient = new();
        private readonly Dictionary<uint, uint> _ownerByEntity = new();
        private readonly List<uint> _clientIds = new();

        #region 属性
        public int MemberCount => _entityByClient.Count;
        #endregion

        /// <summary>
        /// 为新连接分配实体标识（与 clientId 相同）。已加入则失败。
        /// </summary>
        public bool TryJoin(uint clientId, out uint entityId)
        {
            entityId = 0;
            if (clientId == 0) return false;
            if (_entityByClient.ContainsKey(clientId)) return false;

            entityId = clientId;
            _entityByClient.Add(clientId, entityId);
            _ownerByEntity[entityId] = clientId;
            _clientIds.Add(clientId);
            return true;
        }

        public bool TryLeave(uint clientId, out uint entityId)
        {
            if (!_entityByClient.Remove(clientId, out entityId)) return false;
            _ownerByEntity.Remove(entityId);
            _clientIds.Remove(clientId);
            return true;
        }

        public bool TryGetEntityId(uint clientId, out uint entityId)
        {
            return _entityByClient.TryGetValue(clientId, out entityId);
        }

        public bool TryAuthorize(uint clientId, uint entityId)
        {
            return entityId != 0 &&
                _ownerByEntity.TryGetValue(entityId, out uint owner) &&
                owner == clientId;
        }

        public void CopyClientIds(List<uint> destination)
        {
            destination.Clear();
            destination.AddRange(_clientIds);
        }

        public void Clear()
        {
            _entityByClient.Clear();
            _ownerByEntity.Clear();
            _clientIds.Clear();
        }
    }
}
