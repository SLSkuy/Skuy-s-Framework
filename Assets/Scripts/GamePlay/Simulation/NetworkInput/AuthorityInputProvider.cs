using Framework;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 权威网络意图来源：窗口入队，收集口只交出下一拍 InputState。
    /// </summary>
    public sealed class AuthorityInputProvider : IInputStateProvider
    {
        private readonly EntityInputBuffer _buffer;

        #region 属性
        public uint LastProcessedTick => _buffer.LastProcessedTick;
        #endregion

        public AuthorityInputProvider(int capacity, int maxFutureInputTicks)
        {
            _buffer = new EntityInputBuffer(capacity, maxFutureInputTicks);
        }

        public bool Enqueue(uint inputTick, in InputState input)
        {
            return _buffer.Enqueue(inputTick, input);
        }

        public InputState GetInputState()
        {
            if (!TryGetInputState(out InputState input)) return default;
            return input;
        }

        /// <summary>
        /// 取出下一拍已入队的输入。还没有可消费的输入时返回 false，确认点保持不动。
        /// </summary>
        public bool TryGetInputState(out InputState input)
        {
            if (_buffer.LastProcessedTick == 0 && !_buffer.HasQueued)
            {
                input = default;
                return false;
            }

            return _buffer.TryConsumeNext(out input);
        }

        public void Reset()
        {
            _buffer.Reset();
        }
    }
}
