using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络同步根组件，统一负责身份、角色、同步模块注册、控制器装配和同步调度。
    /// </summary>
    [DisallowMultipleComponent]
    public class NetEntitySyncRoot : MonoBehaviour
    {
        [SerializeField] private uint entityId;
        [SerializeField] private NetEntityRole role = NetEntityRole.LocalPlay;

        // 处理所有同步组件
        private readonly List<INetSyncComponent> _modules = new();
        private readonly Dictionary<ModuleType, INetSyncComponent> _moduleMap = new();

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
        public bool IsPredictingOwner => role == NetEntityRole.Predict;
        public bool IsReplica => role == NetEntityRole.Replica;
        public bool IsLocalPlay => role == NetEntityRole.LocalPlay;
        #endregion

        #region 事件
        public event Action<NetEntityRole, NetEntityRole> RoleChanged;
        #endregion

        #region 身份状态
        /// <summary>
        /// 初始化网络实体身份与角色。
        /// </summary>
        public void Init(uint id, NetEntityRole newRole)
        {
            entityId = id;

            NetEntityRole oldRole = role;
            role = newRole;
            if (oldRole != newRole)
            {
                RoleChanged?.Invoke(oldRole, newRole);
            }

            ApplyRole(role);
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
        #endregion

        #region 模块注册
        /// <summary>
        /// 刷新实体及子节点下的同步模块。
        /// </summary>
        public void Refresh()
        {
            _modules.Clear();
            _moduleMap.Clear();

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == this) continue;
                if (behaviour is not INetSyncComponent syncComponent) continue;

                _modules.Add(syncComponent);
                _moduleMap[syncComponent.ModuleType] = syncComponent;
            }
        }

        /// <summary>
        /// 查询指定同步模块。
        /// </summary>
        public bool TryGetModule(ModuleType moduleType, out INetSyncComponent component)
        {
            if (_moduleMap.Count == 0)
            {
                Refresh();
            }

            return _moduleMap.TryGetValue(moduleType, out component);
        }

        /// <summary>
        /// 查询指定类型的同步模块。
        /// </summary>
        public bool TryGetModule<TModule>(ModuleType moduleType, out TModule module)
            where TModule : class, INetSyncComponent
        {
            module = null;
            if (!TryGetModule(moduleType, out INetSyncComponent component)) return false;

            module = component as TModule;
            return module != null;
        }
        #endregion

        #region 角色分发
        
        /// <summary>
        /// 应用网络角色到所有同步模块并装配对应控制器。
        /// </summary>
        private void ApplyRole(NetEntityRole newRole)
        {
            if (_hasAppliedRole && _appliedRole == newRole && _activeController != null) return;

            Refresh();
            foreach (INetSyncComponent component in _modules)
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
        /// 根据角色类型添加对应的控制器
        /// </summary>
        /// <param name="newRole"></param>
        private void ConfigureController(NetEntityRole newRole)
        {
            if (_entity == null) return;

            Type controllerType = ResolveControllerType(newRole);
            if (controllerType == null) return;

            if (_activeController != null && _activeController.GetType() != controllerType)
            {
                _activeController.Unbind();
                _activeController.enabled = false;
                _activeController = null;
            }

            if (_activeController == null)
            {
                _activeController = GetComponent(controllerType) as EntityControllerBase;
                if (_activeController == null)
                {
                    _activeController = gameObject.AddComponent(controllerType) as EntityControllerBase;
                }
            }

            if (_activeController == null) return;

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
        #endregion

        #region 生命周期
        private void Awake()
        {
            _entity = gameObject.GetOrAddComponent<EntityCharacter>();
            
            Refresh();
        }

        private void Start()
        {
            ApplyRole(role);
        }

        private void Update()
        {
            if (role == NetEntityRole.LocalPlay) return;

            foreach (INetSyncComponent component in _modules)
            {
                if (component is INetSyncUpdatable updatable)
                {
                    updatable.SyncUpdate(Time.deltaTime);
                }
            }
        }
        #endregion
    }
}
