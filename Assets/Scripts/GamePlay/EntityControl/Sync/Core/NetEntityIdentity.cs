using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步身份标识器
    /// </summary>
    [DisallowMultipleComponent]
    public class NetEntityIdentity : MonoBehaviour
    {
        [SerializeField] private uint entityId;
        [SerializeField] private NetEntityRole role;

        #region 属性
        public uint EntityId => entityId;
        public NetEntityRole Role => role;
        public bool IsInitialized => entityId != 0;
        public bool IsAuthority => role == NetEntityRole.Authority;
        public bool IsPredictingOwner => role == NetEntityRole.Predict;
        public bool IsReplica => role == NetEntityRole.Replica;
        public bool IsLocalPlay => role == NetEntityRole.LocalPlay;
        #endregion

        #region 事件
        public event Action<NetEntityRole, NetEntityRole> RoleChanged;
        #endregion
        
        /// <summary>
        /// 初始化网络角色
        /// </summary>
        public void Init(uint id, NetEntityRole newRole)
        {
            entityId = id;
            if (role == newRole)
            {
                RoleChanged?.Invoke(role, newRole);
                return;
            }

            SetRole(newRole);
        }

        public void SetRole(NetEntityRole newRole)
        {
            if (role == newRole) return;

            NetEntityRole oldRole = role;
            role = newRole;
            RoleChanged?.Invoke(oldRole, newRole);
        }
    }
}
