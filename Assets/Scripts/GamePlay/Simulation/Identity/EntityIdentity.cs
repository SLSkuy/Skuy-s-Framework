using System;
using Framework;
using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 对象身份识别器，决定如何进行模拟
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityIdentity : MonoBehaviour, IPoolable
    {
        [SerializeField] private uint entityId;
        [SerializeField] private EntityRole role = EntityRole.LocalPlay;
        private Guid _playerId;

        #region 属性
        public uint EntityId => entityId;
        public Guid PlayerId => _playerId;
        public bool IsInitialized => entityId != 0;
        
        public EntityRole Role => role;
        public bool IsAuthority => role == EntityRole.Authority;
        public bool IsPredict => role == EntityRole.Predict;
        public bool IsReplica => role == EntityRole.Replica;
        public bool IsLocalPlay => role == EntityRole.LocalPlay;
        #endregion

        /// <summary>
        /// 初始化网络对象身份
        /// </summary>
        public void Init(uint newEntityId, Guid newPlayerId, EntityRole newRole = EntityRole.LocalPlay)
        {
            if (newEntityId == 0) throw new ArgumentOutOfRangeException(nameof(newEntityId), "NetworkObjectId 不能为 0。");
            if (entityId != 0 && entityId != newEntityId)
            {
                throw new InvalidOperationException($"网络对象 {name} 已绑定 ID {entityId}，不能在激活时改为 {newEntityId}。");
            }

            entityId = newEntityId;
            _playerId = newPlayerId;
            role = newRole;
        }

        /// <summary>
        /// 还池时解开本次实体号，下一次借出可以重新绑定。
        /// </summary>
        void IPoolable.Reset()
        {
            entityId = 0;
            _playerId = Guid.Empty;
            role = EntityRole.LocalPlay;
        }
    }
}
