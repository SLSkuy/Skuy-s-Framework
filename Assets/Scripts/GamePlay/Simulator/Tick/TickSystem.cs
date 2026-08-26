using System;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 固定步长 Tick 模块，支持追帧，避免每帧时长超过Tick时长
    /// </summary>
    public sealed class TickSystem
    {
        private readonly int _maxTicksPerFrame;
        private double _accumulator;
        private readonly double _tickDeltaTime;

        #region 属性
        public bool IsRunning { get; private set; }
        public uint CurrentTick { get; private set; }
        public float TickDeltaTime => (float)_tickDeltaTime;
        public int TickRate { get; }
        #endregion

        #region 事件
        public event Action<uint, float> Tick;
        #endregion

        public TickSystem(int tickRate, int maxTicksPerFrame = 8)
        {
            if (tickRate < 1) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (maxTicksPerFrame < 1) throw new ArgumentOutOfRangeException(nameof(maxTicksPerFrame));
            TickRate = tickRate;
            _tickDeltaTime = 1d / tickRate;
            _maxTicksPerFrame = maxTicksPerFrame;
        }

        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        public void Stop() => IsRunning = false;

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Tick驱动
        /// </summary>
        /// <returns>deltaTime内更新的Tick数量</returns>
        public int Update(double deltaTime)
        {
            if (!IsRunning || deltaTime <= 0d) return 0;
            _accumulator += deltaTime;
            int processedTicks = 0;
            while (_accumulator + 1e-9d >= _tickDeltaTime && processedTicks < _maxTicksPerFrame)
            {
                _accumulator -= _tickDeltaTime;
                CurrentTick++;
                Tick?.Invoke(CurrentTick, (float)_tickDeltaTime);
                processedTicks++;
            }

            return processedTicks;
        }

        public void Reset()
        {
            _accumulator = 0d;
            CurrentTick = 0;
        }
    }
}
