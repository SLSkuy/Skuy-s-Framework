using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 主机权威模拟核：持有 Simulator，开战时生成并附身本地角色。
    /// </summary>
    public sealed class HostSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private Simulator _simulator;

        #region 属性
        public override int Priority => 500;
        public Simulator Simulator => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            
            _simulator.StartClock();
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.StopClock();
            IsSessionRunning = false;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _simulator = new Simulator();
            _simulator.Init();
        }

        public override void Update(float deltaTime)
        {
            _simulator.Update(deltaTime);
        }

        public override void Destroy()
        {
            StopSession();
            _simulator.Destroy();
            _simulator = null;
        }

        #endregion

        #region 网络消息处理

        public void HandlePlayerInput()
        {
            
        }

        #endregion
    }
}
