using System;
using Framework;
using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 对象身份识别器，决定如何进行模拟
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityObjectIdentity : MonoBehaviour, IEntityObjectIdentity, IPoolable
    {
        [SerializeField] private uint entityId;
        [SerializeField] private uint playerId;
        [SerializeField] private EntityObjectRole role = EntityObjectRole.LocalPlay;

        #region 属性
        public uint EntityId => entityId;
        public uint PlayerId => playerId;
        public bool IsInitialized => entityId != 0;
        
        public EntityObjectRole Role => role;
        public bool IsAuthority => role == EntityObjectRole.Authority;
        public bool IsPredict => role == EntityObjectRole.Predict;
        public bool IsReplica => role == EntityObjectRole.Replica;
        public bool IsLocalPlay => role == EntityObjectRole.LocalPlay;
        #endregion

        /// <summary>
        /// 初始化网络对象身份
        /// </summary>
        public void Init(uint newEntityId, uint newPlayerId = 0, EntityObjectRole newRole = EntityObjectRole.LocalPlay)
        {
            if (newEntityId == 0) throw new ArgumentOutOfRangeException(nameof(newEntityId), "NetworkObjectId 不能为 0。");
            if (entityId != 0 && entityId != newEntityId)
            {
                throw new InvalidOperationException($"网络对象 {name} 已绑定 ID {entityId}，不能在运行时改为 {newEntityId}。");
            }

            entityId = newEntityId;
            playerId = newPlayerId;
            role = newRole;
        }

        /// <summary>
        /// 还池时解开本次实体号，下一次借出可以重新绑定。
        /// </summary>
        void IPoolable.Reset()
        {
            entityId = 0;
            playerId = 0;
            role = EntityObjectRole.LocalPlay;
        }
    }
}
