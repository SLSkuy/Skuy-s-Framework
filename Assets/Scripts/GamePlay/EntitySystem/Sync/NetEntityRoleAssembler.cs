using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络角色解释器，根据定义的角色处理同步组件设置
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetEntityIdentity))]
    [RequireComponent(typeof(NetSyncModuleRegistry))]
    public class NetEntityRoleAssembler : MonoBehaviour
    {
        private NetEntityIdentity _identity;
        private NetSyncModuleRegistry _registry;
        private EntityCharacter _character;
        private NetEntityRole _appliedRole;
        private bool _hasAppliedRole;

        public void ApplyRole(NetEntityRole role)
        {
            if (_hasAppliedRole && _appliedRole == role) return;

            _registry.Refresh();
            foreach (INetSyncComponent component in _registry.Modules)
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

        #region 生命周期

        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
            _registry = GetComponent<NetSyncModuleRegistry>();
            _character = GetComponent<EntityCharacter>();
            _identity.RoleChanged += OnRoleChanged;
        }

        private void Start()
        {
            ApplyRole(_identity.Role);
        }

        private void OnDestroy()
        {
            if (_identity != null) _identity.RoleChanged -= OnRoleChanged;
        }

        #endregion
    }
}
