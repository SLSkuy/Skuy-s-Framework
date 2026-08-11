using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体网络同步根组件，负责发现、注册、查询和角色分发同步组件。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetEntityIdentity))]
    public class NetEntitySyncRoot : MonoBehaviour
    {
        private readonly List<INetSyncComponent> _modules = new();
        private readonly Dictionary<ModuleType, INetSyncComponent> _moduleMap = new();

        private NetEntityIdentity _identity;
        private EntityCharacter _character;
        private NetEntityRole _appliedRole;
        private bool _hasAppliedRole;

        #region 属性
        public IReadOnlyList<INetSyncComponent> Modules => _modules;
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
        /// 应用网络角色到所有同步模块。
        /// </summary>
        public void ApplyRole(NetEntityRole role)
        {
            if (_hasAppliedRole && _appliedRole == role) return;

            Refresh();
            foreach (INetSyncComponent component in _modules)
            {
                component.ConfigureRole(role);
            }

            if (_character != null)
            {
                _character.TickDrive = role != NetEntityRole.LocalPlay;
            }

            _appliedRole = role;
            _hasAppliedRole = true;
        }

        private void OnRoleChanged(NetEntityRole oldRole, NetEntityRole newRole)
        {
            ApplyRole(newRole);
        }
        #endregion

        #region 生命周期
        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _character = GetComponent<EntityCharacter>();
            Refresh();
            _identity.RoleChanged += OnRoleChanged;
        }

        private void Start()
        {
            ApplyRole(_identity.Role);
        }

        private void Update()
        {
            if (_identity == null || _identity.Role == NetEntityRole.LocalPlay) return;

            foreach (INetSyncComponent component in _modules)
            {
                if (component is INetSyncUpdatable updatable)
                {
                    updatable.SyncUpdate(Time.deltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            if (_identity != null) _identity.RoleChanged -= OnRoleChanged;
        }
        #endregion
    }
}
