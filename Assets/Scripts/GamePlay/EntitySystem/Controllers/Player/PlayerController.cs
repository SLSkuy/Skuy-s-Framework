using Core;
using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家输入控制器，只负责采样输入并构建固定 Tick 命令。
    /// </summary>
    public class PlayerController : EntityControllerBase
    {
        private readonly EntityInputCommandBuilder _commandBuilder = new();
        private IInputStateProvider _inputProvider;
        private EntitySimulationSystem _simulationSystem;
        private bool _isLocalPlay;

        #region 属性
        public override EntityDriveMode DriveMode => _isLocalPlay ? EntityDriveMode.LocalInput : EntityDriveMode.Prediction;
        #endregion

        /// <summary>
        /// 初始化玩家控制器。
        /// </summary>
        public void Init(BaseEntity entity, NetworkObjectIdentity syncRoot, bool localPlay)
        {
            Bind(entity);
            _isLocalPlay = localPlay;
            if (_isLocalPlay) RegisterLocalSimulation();
        }

        /// <summary>
        /// 设置玩家输入来源。
        /// </summary>
        public void SetInputSource(IInputStateProvider inputProvider)
        {
            _inputProvider = inputProvider;
            _commandBuilder.Reset();
        }

        /// <summary>
        /// 只采样当前输入，不推进本地模拟。用于无预测的服务端权威同步阶段。
        /// </summary>
        public InputState SampleInput()
        {
            return _inputProvider?.GetInputState() ?? default;
        }

        /// <summary>
        /// 采样并立即执行一个预测命令，由外部网络 Tick 驱动。
        /// </summary>
        public InputState CaptureInput(uint inputTick, float tickTime)
        {
            InputState input = SampleInput();
            EntityInputCommand command = _commandBuilder.Build(inputTick, input);
            Target?.Step(inputTick, tickTime, command);
            return input;
        }

        private EntityInputCommand BuildLocalCommand(uint tick)
        {
            InputState input = _inputProvider?.GetInputState() ?? default;
            return _commandBuilder.Build(tick, input);
        }

        private void RegisterLocalSimulation()
        {
            if (Target == null) return;

            SystemManager systemManager = Global.Get<SystemManager>();
            if (systemManager == null) return;

            _simulationSystem = systemManager.GetSystem<EntitySimulationSystem>() ??
                systemManager.RegisterSystem<EntitySimulationSystem>();
            _simulationSystem.RegisterLocal(this, Target, BuildLocalCommand);
        }

        #region 生命周期

        private void Start()
        {
            if (_inputProvider == null && GameCore.Instance != null)
            {
                SetInputSource(GameCore.Instance.LocalInput);
            }
        }

        private void OnDisable()
        {
            _simulationSystem?.UnregisterLocal(this);
            _simulationSystem = null;
        }

        private void OnDestroy()
        {
            _simulationSystem?.UnregisterLocal(this);
        }

        #endregion
    }
}
