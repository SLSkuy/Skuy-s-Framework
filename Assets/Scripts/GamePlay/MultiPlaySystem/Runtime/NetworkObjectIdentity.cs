using System;
using Framework;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 网络对象身份，负责元数据并通知角色复制系统注册。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkObjectIdentity : MonoBehaviour, IEntityObjectIdentity
    {
        [SerializeField] private uint networkObjectId;
        [SerializeField] private uint ownerClientId;
        [SerializeField] private EntitySimulationMode role = EntitySimulationMode.LocalPlay;

        private CharacterReplicationSystem _replicationSystem;

        #region Properties
        public uint NetworkObjectId => networkObjectId;
        public uint EntityId => networkObjectId;
        public uint OwnerClientId => ownerClientId;
        public bool IsInitialized => networkObjectId != 0;
        public EntitySimulationMode Role => role;
        public bool IsAuthority => role == EntitySimulationMode.Authority;
        public bool IsPredict => role == EntitySimulationMode.Predict;
        public bool IsReplica => role == EntitySimulationMode.Replica;
        public bool IsLocalPlay => role == EntitySimulationMode.LocalPlay;
        #endregion

        #region Events
        public event Action<EntitySimulationMode, EntitySimulationMode> RoleChanged;
        #endregion

        /// <summary>
        /// 初始化网络对象身份。
        /// </summary>
        public void Init(uint id, EntitySimulationMode newRole, uint ownerId = 0)
        {
            ApplyNetworkMetadata(id, ownerId, newRole);
        }

        /// <summary>
        /// 应用服务端下发的对象身份、所有者和模拟模式。
        /// </summary>
        public void ApplyNetworkMetadata(uint objectId, uint ownerId, EntitySimulationMode newRole)
        {
            if (objectId == 0) throw new ArgumentOutOfRangeException(nameof(objectId), "NetworkObjectId 不能为 0。");
            if (networkObjectId != 0 && networkObjectId != objectId)
            {
                throw new InvalidOperationException(
                    $"网络对象 {name} 已绑定 ID {networkObjectId}，不能在运行时改为 {objectId}。");
            }

            networkObjectId = objectId;
            ownerClientId = ownerId;
            SetRoleInternal(newRole);
        }

        /// <summary>
        /// 设置网络模拟模式并通知复制系统。
        /// </summary>
        public void SetRole(EntitySimulationMode newRole)
        {
            SetRoleInternal(newRole);
        }

        private void SetRoleInternal(EntitySimulationMode newRole)
        {
            EntitySimulationMode oldRole = role;
            bool roleChanged = oldRole != newRole;
            role = newRole;
            if (roleChanged) RoleChanged?.Invoke(oldRole, newRole);
            RegisterReplication();
        }

        private void RegisterReplication()
        {
            if (networkObjectId == 0 && role != EntitySimulationMode.LocalPlay) return;

            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return;

            _replicationSystem = systemManager.GetSystem<CharacterReplicationSystem>();
            _replicationSystem?.Register(this, ownerClientId);
        }

        #region Lifecycle

        private void OnEnable()
        {
            // Prefab 默认 LocalPlay 且 Id 为 0；网络生成对象必须等 Init 后再注册，避免先按实例 Id 挂上 LocalPlay。
            if (networkObjectId == 0) return;
            RegisterReplication();
        }

        private void Start()
        {
            RegisterReplication();
        }

        private void OnDisable()
        {
            if (_replicationSystem != null)
            {
                _replicationSystem.Unregister(networkObjectId, this);
            }
        }

        #endregion
    }
}
