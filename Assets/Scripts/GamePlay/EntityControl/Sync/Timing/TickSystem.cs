using System;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 固定时间步 Tick 驱动系统
    /// 服务端模拟 / 快照广播 / 客户端输入收集均通过 OnTick 事件驱动
    /// </summary>
    public class TickSystem
    {
        private readonly int _maxTicksPerFrame;
        private float _accumulator;

        #region 属性
        public bool IsRunning { get; private set; }
        public uint CurrentTick { get; private set; }
        public float TickDeltaTime { get; }
        public int TickRate { get; }
        #endregion

        #region 事件
        public event Action<uint> OnTick;
        #endregion

        /// <param name="tickRate">每秒 Tick 数</param>
        /// <param name="maxTicksPerFrame">单帧最大追赶 Tick 数，防止螺旋</param>
        public TickSystem(int tickRate, int maxTicksPerFrame = 10)
        {
            TickRate = Math.Max(1, tickRate);
            TickDeltaTime = 1f / TickRate;
            _maxTicksPerFrame = maxTicksPerFrame;
        }

        /// <summary>
        /// 开始Tick模拟
        /// </summary>
        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        /// <summary>
        /// 关闭Tick模拟
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 由外部每帧调用，驱动固定时间步累加
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            _accumulator += deltaTime;

            int ticksProcessed = 0;
            while (_accumulator >= TickDeltaTime && ticksProcessed < _maxTicksPerFrame)
            {
                _accumulator -= TickDeltaTime;
                CurrentTick++;
                OnTick?.Invoke(CurrentTick);
                ticksProcessed++;
            }

            // 追不上时丢弃剩余时间，防止螺旋恶化
            if (_accumulator >= TickDeltaTime)
            {
                _accumulator = 0f;
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            _accumulator = 0f;
            CurrentTick = 0;
        }
    }
}
