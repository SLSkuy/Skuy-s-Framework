using GamePlay.EntitySystem;
using GamePlay.Simulator;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 显式角色复制条目，绑定身份、模拟、输入源和同步运行时。
    /// </summary>
    public sealed class CharacterReplicationEntry
    {
        private EntityObjectRole _configuredRole;
        private bool _roleConfigured;

        #region 属性
        public EntityObjectIdentity Identity { get; private set; }
        public EntityCharacter Simulation { get; private set; }
        public CharacterPresentationAdapter Presentation { get; private set; }
        public EntityInputBuffer Input { get; private set; }
        public CharacterPredictionController Prediction { get; private set; }
        public CharacterSnapshotInterpolator Interpolation { get; private set; }
        public uint OwnerClientId { get; set; }
        public uint LastAppliedSnapshotTick { get; set; }
        #endregion

        /// <summary>
        /// 绑定角色对象上的显式运行时组件。
        /// </summary>
        public bool TryBind(EntityObjectIdentity identity)
        {
            if (identity == null) return false;

            Identity = identity;
            Simulation = identity.GetComponent<EntityCharacter>();
            Presentation = identity.GetComponent<CharacterPresentationAdapter>();
            if (Simulation == null || Presentation == null)
            {
                Debug.LogError($"网络对象 {identity.name} 缺少 EntityCharacter 或 CharacterPresentationAdapter，无法注册角色复制。");
                return false;
            }

            SimulationConfig config = SimulationConfig.Instance;
            Input ??= new EntityInputBuffer(
                config.maxBufferedInputs, config.maxFutureInputTicks);
            Prediction ??= new CharacterPredictionController(config.predictionHistorySize);
            Interpolation ??= new CharacterSnapshotInterpolator(
                config.simulationTickRate,
                config.interpolationDelayTicks,
                Mathf.Max(8, config.interpolationDelayTicks * 4));
            return true;
        }

        /// <summary>
        /// 按当前模拟角色装配碰撞体与缓冲。
        /// </summary>
        public void ApplyRole(EntityObjectRole role)
        {
            if (Identity == null || Simulation == null) return;
            if (_roleConfigured && _configuredRole == role) return;

            _configuredRole = role;
            _roleConfigured = true;

            if (!Simulation.IsInitialized) Simulation.Init();

            MovementModule movementModule = Identity.GetComponent<MovementModule>();
            bool isReplica = role == EntityObjectRole.Replica;
            movementModule?.SetReplicaMode(isReplica);
            Presentation.ApplyRole(role);

            Input.Reset();
            Prediction.Reset();
            Interpolation.Reset();
            LastAppliedSnapshotTick = 0;
        }
    }
}
