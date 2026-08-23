using Core;
using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家输入控制器，只负责采样输入并构建固定 Tick 命令。
    /// </summary>
    public class PlayerController : EntityControllerBase
    {
        private IInputStateProvider _inputProvider;
        private bool _isLocalPlay;

        #region 属性
        public override EntityDriveMode DriveMode => _isLocalPlay ? EntityDriveMode.LocalInput : EntityDriveMode.Prediction;
        #endregion

        /// <summary>
        /// 初始化玩家控制器。
        /// </summary>
        public void Init(EntitySimulationObject entity, bool localPlay)
        {
            Bind(entity);
            _isLocalPlay = localPlay;
            if (_inputProvider == null && GameCore.Instance != null)
            {
                SetInputSource(GameCore.Instance.LocalInput);
            }
        }

        /// <summary>
        /// 设置玩家输入来源。
        /// </summary>
        public void SetInputSource(IInputStateProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        /// <summary>
        /// 只采样当前输入，不推进本地模拟。用于无预测的服务端权威同步阶段。
        /// </summary>
        public InputState SampleInput()
        {
            return _inputProvider?.GetInputState() ?? default;
        }

        #region 生命周期

        private void Start()
        {
            if (_inputProvider == null && GameCore.Instance != null)
            {
                SetInputSource(GameCore.Instance.LocalInput);
            }
        }

        #endregion
    }
}
