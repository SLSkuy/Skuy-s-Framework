using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 根据网络对象运行模式激活可组合能力组件。
    /// </summary>
    public sealed class NetworkObjectComponentActivator
    {
        private readonly NetworkObjectIdentity _identity;
        private readonly List<INetworkObjectCapability> _capabilities = new();
        private readonly Dictionary<NetworkObjectCapabilityId, INetworkObjectCapability> _capabilitiesById = new();
        private readonly Dictionary<NetworkObjectSyncChannelId, List<INetworkObjectCapability>> _capabilitiesByChannel = new();

        #region 属性
        /// <summary>
        /// 当前对象已发现的能力组件。
        /// </summary>
        public IReadOnlyList<INetworkObjectCapability> Capabilities => _capabilities;
        #endregion

        public NetworkObjectComponentActivator(NetworkObjectIdentity identity)
        {
            _identity = identity;
        }

        /// <summary>
        /// 重新扫描并按运行模式激活能力。
        /// </summary>
        public void Refresh(EntitySimulationMode mode)
        {
            List<INetworkObjectCapability> previousCapabilities = new(_capabilities);
            _capabilities.Clear();
            _capabilitiesById.Clear();
            _capabilitiesByChannel.Clear();

            MonoBehaviour[] components = _identity.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is INetworkObjectCapability capability)
                {
                    if (!_capabilitiesById.TryAdd(capability.CapabilityId, capability))
                    {
                        throw new InvalidOperationException(
                            $"网络对象 {_identity.name} 存在重复能力：{capability.CapabilityId}");
                    }

                    _capabilities.Add(capability);
                    if (!_capabilitiesByChannel.TryGetValue(capability.ChannelId,
                            out List<INetworkObjectCapability> channelCapabilities))
                    {
                        channelCapabilities = new List<INetworkObjectCapability>();
                        _capabilitiesByChannel.Add(capability.ChannelId, channelCapabilities);
                    }
                    channelCapabilities.Add(capability);
                }
            }

            ValidateDependencies();
            foreach (INetworkObjectCapability previousCapability in previousCapabilities)
            {
                if (!_capabilities.Contains(previousCapability)) previousCapability.Deactivate();
            }

            foreach (INetworkObjectCapability capability in _capabilities)
            {
                if (capability.SupportsMode(mode)) capability.Activate(_identity, mode);
                else capability.Deactivate();
            }
        }

        /// <summary>
        /// 获取指定类型的能力组件。
        /// </summary>
        public bool TryGetCapability<TCapability>(out TCapability capability)
            where TCapability : class, INetworkObjectCapability
        {
            foreach (INetworkObjectCapability candidate in _capabilities)
            {
                if (candidate is not TCapability typedCapability) continue;
                capability = typedCapability;
                return true;
            }

            capability = null;
            return false;
        }

        /// <summary>
        /// 获取指定同步 Channel 下的能力集合。
        /// </summary>
        public IReadOnlyList<INetworkObjectCapability> GetChannelCapabilities(NetworkObjectSyncChannelId channelId)
        {
            return _capabilitiesByChannel.TryGetValue(channelId, out List<INetworkObjectCapability> capabilities) ?
                capabilities : Array.Empty<INetworkObjectCapability>();
        }

        /// <summary>
        /// 停用当前对象的全部网络能力。
        /// </summary>
        public void DeactivateAll()
        {
            foreach (INetworkObjectCapability capability in _capabilities) capability.Deactivate();
        }

        private void ValidateDependencies()
        {
            foreach (INetworkObjectCapability capability in _capabilities)
            {
                IReadOnlyList<NetworkObjectCapabilityId> dependencies = capability.RequiredCapabilities;
                for (int i = 0; i < dependencies.Count; i++)
                {
                    if (_capabilitiesById.ContainsKey(dependencies[i])) continue;
                    throw new InvalidOperationException(
                        $"网络对象 {_identity.name} 的能力 {capability.CapabilityId} 缺少依赖：{dependencies[i]}");
                }
            }
        }

    }
}
