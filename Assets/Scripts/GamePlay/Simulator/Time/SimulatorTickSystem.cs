using System;
using Framework;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 全局唯一网络时间服务，为所有模拟模式广播同一固定 Tick。
    /// </summary>
    public sealed class SimulatorTickSystem : SubSystemBase
    {
        private NetworkTickSystem _ticks;

        #region 属性
        public override int Priority => (int)SubSystemPriority.SimulatorTickSystem;
        public uint CurrentTick => _ticks?.CurrentTick ?? 0;
        public float TickDeltaTime => _ticks != null ? (float)_ticks.TickDeltaTime : 0f;
        #endregion

        #region 事件
        public event Action<uint, float> Tick;
        #endregion

        public override void Init()
        {
            SyncConfig config = SyncConfig.Instance;
            _ticks = new NetworkTickSystem(config.simulationTickRate, config.maxSimulationTicksPerFrame);
            _ticks.Tick += HandleTick;
            _ticks.Start();
        }

        public override void Update(float deltaTime)
        {
            _ticks?.Advance(deltaTime);
        }

        public override void Destroy()
        {
            if (_ticks == null) return;
            _ticks.Tick -= HandleTick;
            _ticks.Stop();
        }

        private void HandleTick(uint tick, float deltaTime)
        {
            Tick?.Invoke(tick, deltaTime);
        }
    }
}
