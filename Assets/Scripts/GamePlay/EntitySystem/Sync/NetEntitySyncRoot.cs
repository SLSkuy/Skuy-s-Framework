using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络同步根对象，统一管理角色、同步模块实例和控制器装配。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EntityCharacter))]
    public class NetEntitySyncRoot : MonoBehaviour
    {
        [SerializeField] private uint entityId;
        [SerializeField] private NetEntityRole role = NetEntityRole.LocalPlay;

        // 实体角色能力组件
        private readonly List<EntityModuleBase> _entityModules = new();
        private readonly Dictionary<ModuleType, EntityModuleBase> _entityModuleMap = new();
        
        // 能力网络同步组件
        private readonly List<INetSyncComponent> _syncComponents = new();
        private readonly Dictionary<ModuleType, INetSyncComponent> _syncComponentMap = new();

        private EntityControllerBase _activeController;
        private BaseEntity _entity;
        private NetEntityRole _appliedRole;
        private bool _hasAppliedRole;

        #region 属性
        public uint EntityId => entityId;
        public bool IsInitialized => entityId != 0;
        
        // 网络同步身份
        public NetEntityRole Role => role;
        public bool IsAuthority => role == NetEntityRole.Authority;
        public bool IsPredict => role == NetEntityRole.Predict;
        public bool IsReplica => role == NetEntityRole.Replica;
        public bool IsLocalPlay => role == NetEntityRole.LocalPlay;
        #endregion

        #region 事件
        public event Action<NetEntityRole, NetEntityRole> RoleChanged;
        #endregion
        
        /// <summary>
        /// 初始化网络实体身份与角色。
        /// </summary>
        public void Init(uint id, NetEntityRole newRole)
        {
            entityId = id;
            SetRole(newRole);
        }

        /// <summary>
        /// 设置网络角色。
        /// </summary>
        public void SetRole(NetEntityRole newRole)
        {
            if (role == newRole)
            {
                ApplyRole(role);
                return;
            }

            NetEntityRole oldRole = role;
            role = newRole;
            RoleChanged?.Invoke(oldRole, newRole);
            ApplyRole(role);
        }
        
        /// <summary>
        /// 应用网络角色到所有模块和控制器。
        /// </summary>
        private void ApplyRole(NetEntityRole newRole)
        {
            if (_hasAppliedRole && _appliedRole == newRole && _activeController != null) return;

            // 获取实体能力列表并生成对应的同步组件
            Refresh();
            foreach (INetSyncComponent component in _syncComponents)
            {
                component.ConfigureRole(newRole);
            }

            ConfigureController(newRole);

            if (_entity != null)
            {
                _entity.TickDrive = newRole != NetEntityRole.LocalPlay;
            }

            _appliedRole = newRole;
            _hasAppliedRole = true;
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
        /// 根据实体能力模块，创建并配置同步模块
        /// </summary>
        private void InitSyncModules()
        {
            // TODO 创建并绑定组件
        }
        
        /// <summary>
        /// 获取模块
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
        private void ConfigureController(NetEntityRole newRole)
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

        private Type ResolveControllerType(NetEntityRole newRole)
        {
            return newRole switch
            {
                NetEntityRole.LocalPlay => typeof(PlayerController),
                NetEntityRole.Predict => typeof(PlayerController),
                NetEntityRole.Authority => typeof(AuthorityController),
                NetEntityRole.Replica => typeof(ReplicaController),
                _ => null
            };
        }

        private void InitializeController(EntityControllerBase controller, NetEntityRole newRole)
        {
            switch (controller)
            {
                case PlayerController playerController:
                    playerController.Init(_entity, this, newRole == NetEntityRole.LocalPlay);
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
            // 单机游玩不需要创建同步组件
            if (role == NetEntityRole.LocalPlay) return;

            foreach (INetSyncComponent component in _syncComponents)
            {
                component.Update(Time.deltaTime);
            }
        }
        
        #endregion
    }
}
