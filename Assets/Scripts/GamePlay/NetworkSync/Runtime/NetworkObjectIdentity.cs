using System;
using System.Collections.Generic;
using Framework;
using GamePlay.NetSync;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络同步根对象，统一管理角色、同步模块实例和控制器装配。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityCharacter))]
    public class NetworkObjectIdentity : MonoBehaviour
    {
        [SerializeField] private uint networkObjectId;
        [SerializeField] private uint ownerClientId;
        [SerializeField] private EntitySimulationMode role = EntitySimulationMode.LocalPlay;

        private static readonly Dictionary<ModuleType, Func<EntityModuleBase, INetSyncComponent>> SyncComponentFactories = new()
        {
            { ModuleType.Position, module => new NetPositionSync((MovementModule)module) },
        };

        // 实体角色能力组件
        private readonly List<EntityModuleBase> _entityModules = new();
        private readonly Dictionary<ModuleType, EntityModuleBase> _entityModuleMap = new();

        // 能力网络同步组件
        private readonly List<INetSyncComponent> _syncComponents = new();
        private readonly Dictionary<ModuleType, INetSyncComponent> _syncComponentMap = new();

        private EntityControllerBase _activeController;
        private BaseEntity _entity;
        private EntitySimulationMode _appliedRole;
        private bool _hasAppliedRole;
        private EntityReplicationSystem _replicationSystem;

        #region 属性
        public uint NetworkObjectId => networkObjectId;
        public uint EntityId => networkObjectId;
        public uint OwnerClientId => ownerClientId;
        public bool IsInitialized => networkObjectId != 0;
        
        // 网络同步身份
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
        /// 初始化网络实体身份与角色。
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
        /// 设置网络角色。
        /// </summary>
        public void SetRole(EntitySimulationMode newRole)
        {
            if (role == newRole)
            {
                ApplyRole(role);
                return;
            }

            EntitySimulationMode oldRole = role;
            role = newRole;
            RoleChanged?.Invoke(oldRole, newRole);
            ApplyRole(role);
        }

        /// <summary>
        /// 应用网络角色到所有模块和控制器。
        /// </summary>
        private void ApplyRole(EntitySimulationMode newRole)
        {
            if (_hasAppliedRole && _appliedRole == newRole && _activeController != null) return;

            Refresh();
            foreach (INetSyncComponent component in _syncComponents)
            {
                component.ConfigureRole(newRole);
            }

            ConfigureController(newRole);

            _appliedRole = newRole;
            _hasAppliedRole = true;
            RegisterReplication();
        }

        /// <summary>
        /// 刷新实体能力模块与同步模块。
        /// </summary>
        public void Refresh()
        {
            CollectEntityModules();
            InitSyncModules();
        }

        /// <summary>
        /// 收集实体能力模块。
        /// </summary>
        private void CollectEntityModules()
        {
            _entityModules.Clear();
            _entityModuleMap.Clear();

            EntityModuleBase[] modules = GetComponentsInChildren<EntityModuleBase>(true);
            foreach (EntityModuleBase module in modules)
            {
                if (module == null) continue;
                _entityModules.Add(module);
                _entityModuleMap[module.ModuleType] = module;
            }
        }

        /// <summary>
        /// 根据实体能力模块，创建并配置同步模块。
        /// </summary>
        private void InitSyncModules()
        {
            _syncComponents.Clear();
            _syncComponentMap.Clear();

            foreach (EntityModuleBase module in _entityModules)
            {
                TryAddSyncComponent(module);
            }
        }

        /// <summary>
        /// 尝试创建并登记同步组件。
        /// </summary>
        private void TryAddSyncComponent(EntityModuleBase module)
        {
            if (module == null || _syncComponentMap.ContainsKey(module.ModuleType)) return;

            INetSyncComponent syncComponent = CreateSyncComponent(module);
            if (syncComponent == null)
            {
                Debug.LogWarning($"[{nameof(NetworkObjectIdentity)}] {name} 上的 {module.GetType().Name} 没有对应的同步组件。", this);
                return;
            }

            syncComponent.Bind(this);
            _syncComponents.Add(syncComponent);
            _syncComponentMap[module.ModuleType] = syncComponent;
        }

        /// <summary>
        /// 创建与能力模块对应的同步组件。
        /// </summary>
        private INetSyncComponent CreateSyncComponent(EntityModuleBase module)
        {
            if (!SyncComponentFactories.TryGetValue(module.ModuleType, out Func<EntityModuleBase, INetSyncComponent> factory))
                return null;

            try
            {
                return factory(module);
            }
            catch (InvalidCastException)
            {
                Debug.LogError($"[{nameof(NetworkObjectIdentity)}] {name} 的 {module.ModuleType} 同步工厂类型不匹配：{module.GetType().Name}。", this);
                return null;
            }
        }

        /// <summary>
        /// 获取同步模块。
        /// </summary>
        public bool TryGetModule<T>(ModuleType moduleType, out T component)
            where T : class, INetSyncComponent
        {
            component = null;

            if (_syncComponentMap.Count == 0)
            {
                Refresh();
            }

            if (!_syncComponentMap.TryGetValue(moduleType, out var baseComponent))
                return false;

            if (baseComponent is not T typedComponent)
                return false;

            component = typedComponent;
            return true;
        }

        /// <summary>
        /// 按角色装配控制器。
        /// </summary>
        private void ConfigureController(EntitySimulationMode newRole)
        {
            if (!_entity) return;

            Type controllerType = ResolveControllerType(newRole);
            if (controllerType == null) return;

            if (_activeController && _activeController.GetType() != controllerType)
            {
                _activeController.Unbind();
                _activeController.enabled = false;
                _activeController = null;
            }

            if (!_activeController)
            {
                _activeController = gameObject.GetComponent(controllerType) as EntityControllerBase;
                if (!_activeController)
                {
                    _activeController = gameObject.AddComponent(controllerType) as EntityControllerBase;
                }
            }

            if (!_activeController) return;

            _activeController.enabled = true;
            InitializeController(_activeController, newRole);
        }

        private Type ResolveControllerType(EntitySimulationMode newRole)
        {
            return newRole switch
            {
                EntitySimulationMode.LocalPlay => typeof(PlayerController),
                EntitySimulationMode.Predict => typeof(PlayerController),
                EntitySimulationMode.Authority => typeof(AuthorityController),
                EntitySimulationMode.Replica => typeof(ReplicaController),
                _ => null
            };
        }

        private void InitializeController(EntityControllerBase controller, EntitySimulationMode newRole)
        {
            switch (controller)
            {
                case PlayerController playerController:
                    playerController.Init(_entity, this, newRole == EntitySimulationMode.LocalPlay);
                    break;
                case AuthorityController authorityController:
                    authorityController.Init(_entity, this);
                    break;
                case ReplicaController replicaController:
                    replicaController.Init(_entity, this);
                    break;
                default:
                    controller.Bind(_entity);
                    break;
            }
        }

        #region 生命周期

        private void Awake()
        {
            _entity = gameObject.GetOrAddComponent<EntityCharacter>();
            _entity.Init();
        }

        private void Start()
        {
            ApplyRole(role);
        }

        private void Update()
        {
            if (role == EntitySimulationMode.LocalPlay) return;

            foreach (INetSyncComponent component in _syncComponents)
            {
                component.Update(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (_replicationSystem != null && networkObjectId != 0)
            {
                _replicationSystem.Unregister(networkObjectId);
            }
        }

        private void OnEnable()
        {
            if (_hasAppliedRole) RegisterReplication();
        }

        private void RegisterReplication()
        {
            if (_entity == null || networkObjectId == 0 || role == EntitySimulationMode.LocalPlay) return;

            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return;

            _replicationSystem = systemManager.GetSystem<EntityReplicationSystem>();
            _replicationSystem?.Register(_entity, this, ownerClientId);
        }

        #endregion
    }
}
