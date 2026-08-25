using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 显式角色复制条目，绑定身份、模拟、输入源和同步运行时。
    /// </summary>
    public sealed class CharacterReplicationEntry
    {
        private EntitySimulationMode _configuredRole;
        private bool _roleConfigured;

        #region Properties
        public NetworkObjectIdentity Identity { get; private set; }
        public EntityCharacter Simulation { get; private set; }
        public PlayerController PlayerController { get; private set; }
        public AIController AIController { get; private set; }
        public CharacterPresentationAdapter Presentation { get; private set; }
        public CharacterInputBuffer Input { get; private set; }
        public CharacterPredictionController Prediction { get; private set; }
        public CharacterSnapshotInterpolator Interpolation { get; private set; }
        public uint OwnerClientId { get; set; }
        public uint LastAppliedSnapshotTick { get; set; }
        #endregion

        /// <summary>
        /// 绑定角色对象上的显式运行时组件。
        /// </summary>
        public bool TryBind(NetworkObjectIdentity identity)
        {
            if (identity == null) return false;

            Identity = identity;
            Simulation = identity.GetComponent<EntityCharacter>();
            Presentation = identity.GetComponent<CharacterPresentationAdapter>();
            PlayerController = identity.GetComponent<PlayerController>();
            AIController = identity.GetComponent<AIController>();
            if (Simulation == null || Presentation == null)
            {
                Debug.LogError($"网络对象 {identity.name} 缺少 EntityCharacter 或 CharacterPresentationAdapter，无法注册角色复制。");
                return false;
            }

            SyncConfig config = SyncConfig.Instance;
            Input ??= new CharacterInputBuffer(
                config.maxBufferedInputs, config.maxFutureInputTicks);
            Prediction ??= new CharacterPredictionController(config.predictionHistorySize);
            Interpolation ??= new CharacterSnapshotInterpolator(
                config.simulationTickRate,
                config.interpolationDelayTicks,
                Mathf.Max(8, config.interpolationDelayTicks * 4));
            return true;
        }

        /// <summary>
        /// 按当前模拟角色装配输入源、碰撞体与缓冲。
        /// </summary>
        public void ApplyRole(EntitySimulationMode role)
        {
            if (Identity == null || Simulation == null) return;
            if (_roleConfigured && _configuredRole == role)
            {
                UpdateControllerActivation(role);
                return;
            }

            _configuredRole = role;
            _roleConfigured = true;

            if (!Simulation.IsInitialized) Simulation.Init();

            MovementModule movementModule = Identity.GetComponent<MovementModule>();
            bool isReplica = role == EntitySimulationMode.Replica;
            movementModule?.SetReplicaMode(isReplica);
            Presentation.ApplyRole(role);

            Input.Reset();
            Prediction.Reset();
            Interpolation.Reset();
            LastAppliedSnapshotTick = 0;

            UpdateControllerActivation(role);
        }

        private void UpdateControllerActivation(EntitySimulationMode role)
        {
            bool requiresLocalInput = role == EntitySimulationMode.Predict ||
                role == EntitySimulationMode.LocalPlay;
            if (PlayerController != null)
            {
                PlayerController.enabled = requiresLocalInput;
                if (requiresLocalInput) PlayerController.Bind(Simulation);
            }

            if (AIController != null) AIController.enabled = role == EntitySimulationMode.Authority && OwnerClientId == 0;
        }
    }
}
