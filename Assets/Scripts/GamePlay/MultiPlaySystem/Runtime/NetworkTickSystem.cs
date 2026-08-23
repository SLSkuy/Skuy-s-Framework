using System;

namespace GamePlay.MultiPlaySystem
{
    public sealed class NetworkTickSystem
    {
        private readonly int _maxTicksPerFrame;
        private double _accumulator;

        #region Properties
        public bool IsRunning { get; private set; }
        public uint CurrentTick { get; private set; }
        public double TickDeltaTime { get; }
        public int TickRate { get; }
        public uint CatchUpLimitCount { get; private set; }
        #endregion

        #region Events
        public event Action<uint, float> Tick;
        #endregion

        public NetworkTickSystem(int tickRate, int maxTicksPerFrame = 8)
        {
            if (tickRate < 1) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (maxTicksPerFrame < 1) throw new ArgumentOutOfRangeException(nameof(maxTicksPerFrame));
            TickRate = tickRate;
            TickDeltaTime = 1d / tickRate;
            _maxTicksPerFrame = maxTicksPerFrame;
        }

        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        public void Stop() => IsRunning = false;

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

        public void Reset()
        {
            _accumulator = 0d;
            CurrentTick = 0;
            CatchUpLimitCount = 0;
        }
    }
}
