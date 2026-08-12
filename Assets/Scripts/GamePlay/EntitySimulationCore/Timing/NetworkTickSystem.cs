using System;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 全局固定 Tick 时间源，负责受限追赶与超限统计。
    /// </summary>
    public sealed class NetworkTickSystem
    {
        private readonly int _maxTicksPerFrame;
        private double _accumulator;

        #region 属性
        public bool IsRunning { get; private set; }
        public uint CurrentTick { get; private set; }
        public double TickDeltaTime { get; }
        public int TickRate { get; }
        public uint CatchUpLimitCount { get; private set; }
        #endregion

        #region 事件
        public event Action<uint, float> Tick;
        #endregion

        /// <summary>
        /// 创建固定 Tick 时间源。
        /// </summary>
        public NetworkTickSystem(int tickRate, int maxTicksPerFrame = 8)
        {
            if (tickRate < 1) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (maxTicksPerFrame < 1) throw new ArgumentOutOfRangeException(nameof(maxTicksPerFrame));

            TickRate = tickRate;
            TickDeltaTime = 1d / tickRate;
            _maxTicksPerFrame = maxTicksPerFrame;
        }

        /// <summary>
        /// 从 Tick 0 开始运行。
        /// </summary>
        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        /// <summary>
        /// 停止推进，保留当前 Tick。
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 累积渲染帧时间并执行零到多个固定 Tick。
        /// </summary>
        public int Advance(double deltaTime)
        {
            if (!IsRunning || deltaTime <= 0d) return 0;

            _accumulator += deltaTime;
            int processedTicks = 0;
            while (_accumulator + 1e-9d >= TickDeltaTime && processedTicks < _maxTicksPerFrame)
            {
                _accumulator -= TickDeltaTime;
                CurrentTick++;
                Tick?.Invoke(CurrentTick, (float)TickDeltaTime);
                processedTicks++;
            }

            if (_accumulator + 1e-9d >= TickDeltaTime)
            {
                CatchUpLimitCount++;
                _accumulator %= TickDeltaTime;
            }

            return processedTicks;
        }

        /// <summary>
        /// 清空时间和统计状态。
        /// </summary>
        public void Reset()
        {
            _accumulator = 0d;
            CurrentTick = 0;
            CatchUpLimitCount = 0;
        }
    }
}
