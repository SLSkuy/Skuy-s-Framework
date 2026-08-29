using Framework;

namespace GamePlay.Simulator
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
            if (_buffer.LastProcessedTick == 0 && !_buffer.HasQueued) return default;
            return _buffer.ConsumeNext();
        }

        public void Reset()
        {
            _buffer.Reset();
        }
    }
}
