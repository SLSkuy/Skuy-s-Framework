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

        public NetworkObjectComponentActivator(NetworkObjectIdentity identity)
        {
            _identity = identity;
        }

        /// <summary>
        /// 当前对象已发现的能力组件。
        /// </summary>
        public IReadOnlyList<INetworkObjectCapability> Capabilities => _capabilities;

        /// <summary>
        /// 重新扫描并按运行模式激活能力。
        /// </summary>
        public void Refresh(EntitySimulationMode mode)
        {
            _capabilities.Clear();

            MonoBehaviour[] components = _identity.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is INetworkObjectCapability capability)
                {
                    _capabilities.Add(capability);
                    if (ShouldActivate(capability.CapabilityId, mode))
                    {
                        capability.Activate(_identity, mode);
                    }
                    else
                    {
                        capability.Deactivate();
                    }
                }
            }
        }

        private static bool ShouldActivate(NetworkObjectCapabilityId capabilityId, EntitySimulationMode mode)
        {
            return capabilityId switch
            {
                NetworkObjectCapabilityId.Transform => true,
                NetworkObjectCapabilityId.Simulation => mode != EntitySimulationMode.Replica,
                NetworkObjectCapabilityId.Snapshot => mode != EntitySimulationMode.LocalPlay,
                NetworkObjectCapabilityId.Prediction => mode == EntitySimulationMode.Predict,
                NetworkObjectCapabilityId.Interpolation => mode == EntitySimulationMode.Replica,
                _ => false
            };
        }
    }
}
