using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络对象能力组件的通用激活状态基类。
    /// </summary>
    public abstract class NetworkObjectCapabilityBase : MonoBehaviour, INetworkObjectCapability
    {
        private NetworkObjectIdentity _identity;
        private EntitySimulationMode _activeMode;
        private bool _isActive;

        #region 属性
        public abstract NetworkObjectCapabilityId CapabilityId { get; }
        public abstract NetworkObjectSyncChannelId ChannelId { get; }
        public virtual IReadOnlyList<NetworkObjectCapabilityId> RequiredCapabilities =>
            Array.Empty<NetworkObjectCapabilityId>();
        public bool IsActive => _isActive;
        protected NetworkObjectIdentity Identity => _identity;
        #endregion

        /// <summary>
        /// 判断能力是否应在指定运行模式下启用。
        /// </summary>
        public abstract bool SupportsMode(EntitySimulationMode mode);

        public void Activate(NetworkObjectIdentity identity, EntitySimulationMode mode)
        {
            if (_isActive && _identity == identity && _activeMode == mode) return;
            if (_isActive) Deactivate();

            _identity = identity;
            _activeMode = mode;
            _isActive = true;
            enabled = true;
            OnActivated(mode);
        }

        public void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;
            OnDeactivated();
            enabled = false;
            _identity = null;
        }

        protected virtual void OnActivated(EntitySimulationMode mode)
        {
        }

        protected virtual void OnDeactivated()
        {
        }
    }
}
