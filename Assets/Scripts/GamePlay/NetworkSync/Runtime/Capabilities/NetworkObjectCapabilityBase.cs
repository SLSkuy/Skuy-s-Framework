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
        private bool _isActive;

        #region 属性
        public abstract NetworkObjectCapabilityId CapabilityId { get; }
        public abstract NetworkObjectSyncChannelId ChannelId { get; }
        public bool IsActive => _isActive;
        protected NetworkObjectIdentity Identity => _identity;
        #endregion

        public void Activate(NetworkObjectIdentity identity, EntitySimulationMode mode)
        {
            _identity = identity;
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
        }

        protected virtual void OnActivated(EntitySimulationMode mode)
        {
        }

        protected virtual void OnDeactivated()
        {
        }
    }
}
