using System;
using UnityEngine;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 对象身份识别器，决定如何进行模拟
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityObjectIdentity : MonoBehaviour, IEntityObjectIdentity
    {
        [SerializeField] private uint entityId;
        [SerializeField] private uint ownerClientId;
        [SerializeField] private EntityObjectRole role = EntityObjectRole.LocalPlay;

        #region 属性
        public uint EntityId => entityId;
        public uint OwnerClientId => ownerClientId;
        public bool IsInitialized => entityId != 0;
        
        public EntityObjectRole Role => role;
        public bool IsAuthority => role == EntityObjectRole.Authority;
        public bool IsPredict => role == EntityObjectRole.Predict;
        public bool IsReplica => role == EntityObjectRole.Replica;
        public bool IsLocalPlay => role == EntityObjectRole.LocalPlay;
        #endregion

        #region 事件
        public event Action<EntityObjectRole, EntityObjectRole> RoleChanged;
        #endregion

        /// <summary>
        /// 初始化网络对象身份
        /// </summary>
        public void Init(uint objectId, uint ownerId = 0, EntityObjectRole newRole = EntityObjectRole.LocalPlay)
        {
            if (objectId == 0) throw new ArgumentOutOfRangeException(nameof(objectId), "NetworkObjectId 不能为 0。");
            if (entityId != 0 && entityId != objectId)
            {
                throw new InvalidOperationException($"网络对象 {name} 已绑定 ID {entityId}，不能在运行时改为 {objectId}。");
            }

            entityId = objectId;
            ownerClientId = ownerId;
            SetRole(newRole);
        }

        /// <summary>
        /// 设置网络模拟模式并通知复制系统
        /// </summary>
        public void SetRole(EntityObjectRole newRole)
        {
            EntityObjectRole oldRole = role;
            bool roleChanged = oldRole != newRole;
            role = newRole;
            if (roleChanged) RoleChanged?.Invoke(oldRole, newRole);
        }
    }
}
