using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 网络意图来源：仅在源内持有 InputState，缺采样返回空快照。
    /// </summary>
    public sealed class NetworkInputProvider : IInputStateProvider
    {
        private InputState _pending;
        private bool _hasPending;

        /// <summary>
        /// 覆盖写入本拍网络快照。
        /// </summary>
        public void Enqueue(in InputState state)
        {
            _pending = state;
            _hasPending = true;
        }

        /// <summary>
        /// 取出已入队快照；无采样时返回空输入。
        /// </summary>
        public InputState GetInputState()
        {
            if (!_hasPending) return default;
            _hasPending = false;
            InputState state = _pending;
            _pending = default;
            return state;
        }
    }
}
