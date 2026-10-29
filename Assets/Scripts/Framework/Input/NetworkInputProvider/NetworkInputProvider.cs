namespace Framework
{
    /// <summary>
    /// 权威网络输入来源：按客户端 Tick 入队，向模拟层提供下一拍 InputCommand。
    /// </summary>
    public sealed class NetworkInputProvider : IInputProvider
    {
        private readonly NetworkInputBuffer _buffer;

        #region 属性
        public uint LastProcessedTick => _buffer.LastProcessedTick;
        #endregion

        public NetworkInputProvider(int capacity, int maxFutureInputTicks)
        {
            _buffer = new NetworkInputBuffer(capacity, maxFutureInputTicks);
        }

        /// <summary>
        /// 将客户端输入加入接收窗口，拒绝过期或超出窗口的输入。
        /// </summary>
        public bool Enqueue(uint inputTick, in InputCommand input)
        {
            return _buffer.Enqueue(inputTick, input);
        }

        /// <summary>
        /// 获取下一拍输入；等待输入时返回空命令。
        /// 需要区分等待与空命令的调用方应使用 TryGetInputCommand。
        /// </summary>
        public InputCommand GetInputCommand()
        {
            TryGetInputCommand(out InputCommand input);
            return input;
        }

        /// <summary>
        /// 取出下一拍输入；等待时返回 false，连续缺席超过上限后返回空命令并推进确认点。
        /// </summary>
        public bool TryGetInputCommand(out InputCommand input)
        {
            return _buffer.TryConsumeNext(out input);
        }

        /// <summary>
        /// 清空输入缓冲与消费进度。
        /// </summary>
        public void Reset()
        {
            _buffer.Reset();
        }
    }
}