using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体身份组件，客户端所有权由可选的NetworkOwnership组件描述
    /// </summary>
    public class NetEntity : MonoBehaviour, INetEntity
    {
        [Header("网络同步标识")]
        [SerializeField] private uint entityId;
        [SerializeField] private NetEntityRole role = NetEntityRole.Authority;

        public uint EntityId => entityId;
        public NetEntityRole Role => role;
        public bool IsInitialized => entityId != 0;
        public bool HasStateAuthority => role == NetEntityRole.Authority;

        public event Action<NetEntityRole, NetEntityRole> RoleChanged;

        public void Init(uint newEntityId, NetEntityRole newRole)
        {
            if (newEntityId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newEntityId), "Entity id must be non-zero.");
            }

            if (IsInitialized && entityId != newEntityId)
            {
                throw new InvalidOperationException($"Entity {entityId} cannot be reinitialized as {newEntityId}.");
            }

            entityId = newEntityId;
            SetRole(newRole);
        }

        public void SetRole(NetEntityRole newRole)
        {
            if (role == newRole) return;

            NetEntityRole previousRole = role;
            role = newRole;
            OnRoleChanged(previousRole, newRole);
            RoleChanged?.Invoke(previousRole, newRole);
        }

        /// <summary>
        /// 同步角色发生变化
        /// </summary>
        protected virtual void OnRoleChanged(NetEntityRole previousRole, NetEntityRole newRole)
        {
            // TODO: 啥也不做
        }
    }
}
