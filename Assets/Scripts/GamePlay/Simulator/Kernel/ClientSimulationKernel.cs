using Framework;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 客户端模拟核占位：预测与快照接入前只维持会话，不驱动权威步进。
    /// </summary>
    public sealed class ClientSimulationKernel : SubSystemBase, ISimulationKernel
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
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;
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
    }
}
