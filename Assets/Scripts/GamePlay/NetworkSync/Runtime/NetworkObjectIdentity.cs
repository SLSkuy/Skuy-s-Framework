using System;
using System.Collections.Generic;
using Framework;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 通用网络对象身份，负责元数据、能力激活和复制系统注册。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkObjectIdentity : MonoBehaviour
    {
        [SerializeField] private uint networkObjectId;
        [SerializeField] private uint ownerClientId;
        [SerializeField] private EntitySimulationMode role = EntitySimulationMode.LocalPlay;

        private EntityReplicationSystem _replicationSystem;
        private NetworkObjectComponentActivator _componentActivator;
        private bool _capabilitiesInitialized;

        #region 属性
        public uint NetworkObjectId => networkObjectId;
        public uint EntityId => networkObjectId;
        public uint OwnerClientId => ownerClientId;
        public bool IsInitialized => networkObjectId != 0;
        public EntitySimulationMode Role => role;
        public bool IsAuthority => role == EntitySimulationMode.Authority;
        public bool IsPredict => role == EntitySimulationMode.Predict;
        public bool IsReplica => role == EntitySimulationMode.Replica;
        public bool IsLocalPlay => role == EntitySimulationMode.LocalPlay;
        public IReadOnlyList<INetworkObjectCapability> Capabilities =>
            _componentActivator?.Capabilities ?? Array.Empty<INetworkObjectCapability>();
        #endregion

        #region 事件
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

            bool identityChanged = networkObjectId != objectId;
            networkObjectId = objectId;
            ownerClientId = ownerId;
            SetRoleInternal(newRole, identityChanged || !_capabilitiesInitialized);
        }

        /// <summary>
        /// 设置网络模拟模式并刷新能力启用矩阵。
        /// </summary>
        public void SetRole(EntitySimulationMode newRole)
        {
            SetRoleInternal(newRole, !_capabilitiesInitialized);
        }

        /// <summary>
        /// 获取指定类型的网络能力组件。
        /// </summary>
        public bool TryGetCapability<TCapability>(out TCapability capability)
            where TCapability : class, INetworkObjectCapability
        {
            return _componentActivator.TryGetCapability(out capability);
        }

        /// <summary>
        /// 获取指定同步 Channel 下的能力集合。
        /// </summary>
        public IReadOnlyList<INetworkObjectCapability> GetChannelCapabilities(NetworkObjectSyncChannelId channelId)
        {
            return _componentActivator.GetChannelCapabilities(channelId);
        }

        private void SetRoleInternal(EntitySimulationMode newRole, bool forceRefresh)
        {
            EntitySimulationMode oldRole = role;
            bool roleChanged = oldRole != newRole;
            role = newRole;

            if (forceRefresh || roleChanged)
            {
                _componentActivator.Refresh(role);
                _capabilitiesInitialized = true;
            }

            if (roleChanged) RoleChanged?.Invoke(oldRole, newRole);
            RegisterReplication();
        }

        private void RegisterReplication()
        {
            if (!_capabilitiesInitialized || networkObjectId == 0 || role == EntitySimulationMode.LocalPlay) return;

            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return;

            _replicationSystem = systemManager.GetSystem<EntityReplicationSystem>();
            _replicationSystem?.Register(this, ownerClientId);
        }

        #region 生命周期

        private void Awake()
        {
            _componentActivator = new NetworkObjectComponentActivator(this);
        }

        private void OnEnable()
        {
            if (networkObjectId == 0) return;
            _componentActivator.Refresh(role);
            _capabilitiesInitialized = true;
            RegisterReplication();
        }

        private void Start()
        {
            RegisterReplication();
        }

        private void OnDisable()
        {
            if (_replicationSystem != null && networkObjectId != 0)
            {
                _replicationSystem.Unregister(networkObjectId, this);
            }

            _componentActivator?.DeactivateAll();
            _capabilitiesInitialized = false;
        }

        #endregion
    }
}
