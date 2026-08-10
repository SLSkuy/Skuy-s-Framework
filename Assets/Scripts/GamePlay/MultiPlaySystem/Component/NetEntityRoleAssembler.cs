using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络角色解释器，根据定义的角色处理同步组件设置
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetEntityIdentity))]
    public class NetEntityRoleAssembler : MonoBehaviour
    {
        private readonly List<INetSyncComponent> _syncComponents = new();
        private NetEntityIdentity _identity;
        private EntityCharacter _character;
        private NetEntityRole _appliedRole;
        private bool _hasAppliedRole;

        public void ApplyRole(NetEntityRole role)
        {
            if (_hasAppliedRole && _appliedRole == role) return;

            RefreshComponents();
            foreach (INetSyncComponent component in _syncComponents)
            {
                component.ConfigureRole(role);
            }

            if (_character != null)
            {
                _character.tickDrive = role != NetEntityRole.LocalPlay;
            }

            _appliedRole = role;
            _hasAppliedRole = true;
        }

        private void RefreshComponents()
        {
            _syncComponents.Clear();
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is INetSyncComponent syncComponent)
                {
                    _syncComponents.Add(syncComponent);
                }
            }
        }

        private void OnRoleChanged(NetEntityRole oldRole, NetEntityRole newRole)
        {
            ApplyRole(newRole);
        }

        #region 生命周期

        private void Awake()
        {
            _identity = GetComponent<NetEntityIdentity>();
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
