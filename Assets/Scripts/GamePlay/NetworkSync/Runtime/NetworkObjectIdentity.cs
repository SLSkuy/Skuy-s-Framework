using System;
using Framework;
using GamePlay.NetSync;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络身份，负责元数据、角色和复制系统注册。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityCharacter))]
    public sealed class NetworkObjectIdentity : MonoBehaviour
    {
        [SerializeField] private uint networkObjectId;
        [SerializeField] private uint ownerClientId;
        [SerializeField] private EntitySimulationMode role = EntitySimulationMode.LocalPlay;

        private BaseEntity _entity;
        private EntityReplicationSystem _replicationSystem;
        private PlayerController _playerController;
        private NetworkObjectComponentActivator _componentActivator;

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
        #endregion

        #region 事件
        public event Action<EntitySimulationMode, EntitySimulationMode> RoleChanged;
        #endregion

        /// <summary>
        /// 初始化网络实体身份。
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
            networkObjectId = objectId;
            ownerClientId = ownerId;
            SetRole(newRole);
        }

        /// <summary>
        /// 设置网络模拟模式并装配本地输入控制器。
        /// </summary>
        public void SetRole(EntitySimulationMode newRole)
        {
            if (role != newRole)
            {
                EntitySimulationMode oldRole = role;
                role = newRole;
                RoleChanged?.Invoke(oldRole, newRole);
            }

            _componentActivator?.Refresh(role);
            ConfigureMovementMode();
            ConfigurePlayerController();
            RegisterReplication();
        }

        private void ConfigureMovementMode()
        {
            MovementModule movementModule = GetComponent<MovementModule>();
            movementModule?.SetReplicaMode(role == EntitySimulationMode.Replica);
        }

        private void ConfigurePlayerController()
        {
            bool needsPlayerController = role == EntitySimulationMode.LocalPlay || role == EntitySimulationMode.Predict;
            if (!needsPlayerController)
            {
                if (_playerController != null) _playerController.enabled = false;
                return;
            }

            _playerController ??= gameObject.GetOrAddComponent<PlayerController>();
            _playerController.enabled = true;
            _playerController.Init(_entity, this, role == EntitySimulationMode.LocalPlay);
        }

        private void RegisterReplication()
        {
            if (_entity == null || networkObjectId == 0 || role == EntitySimulationMode.LocalPlay) return;

            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return;

            _replicationSystem = systemManager.GetSystem<EntityReplicationSystem>();
            _replicationSystem?.Register(_entity, this, ownerClientId);
        }

        #region 生命周期

        private void Awake()
        {
            _entity = gameObject.GetOrAddComponent<EntityCharacter>();
            _entity.Init();
            _componentActivator = new NetworkObjectComponentActivator(this);
            _componentActivator.Refresh(role);
        }

        private void Start()
        {
            ConfigureMovementMode();
            ConfigurePlayerController();
            RegisterReplication();
        }

        private void OnEnable()
        {
            RegisterReplication();
        }

        private void OnDisable()
        {
            if (_replicationSystem != null && networkObjectId != 0)
            {
                _replicationSystem.Unregister(networkObjectId);
            }
        }

        #endregion
    }
}
