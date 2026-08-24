using Core;
using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家输入控制器，只负责采样输入
    /// </summary>
    public class PlayerController : EntityControllerBase
    {
        private IInputStateProvider _inputProvider;

        /// <summary>
        /// 设置输入源
        /// </summary>
        public void SetInputSource(IInputStateProvider inputProvider)
        {
            _inputProvider = inputProvider;
        }

        /// <summary>
        /// 采样当前玩家输入
        /// </summary>
        /// <returns></returns>
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
