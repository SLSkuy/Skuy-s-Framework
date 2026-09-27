using Framework;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端模拟核：沿用模拟核节拍，在移动输入时机把本机玩家输入发给主机。
    /// </summary>
    public sealed class ClientSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private IInputStateProvider _localInputProvider;
        private SimulationClientHandler _clientHandler;
        private Simulator _simulator;

        #region 属性
        public override int Priority => 500;
        public Simulator SimulationKernal => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            
            _localInputProvider = Global.Get<LocalInputManager>().Provider;
            
            _clientHandler = new SimulationClientHandler();
            _clientHandler.Bind();
            
            _simulator.TickPlayerInput += HandlePlayerInputTick;
            _simulator.StartClock();
            
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.TickPlayerInput -= HandlePlayerInputTick;
            _simulator.StopClock();
            
            _clientHandler.Unbind();
            _clientHandler = null;
            
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

        #region 回调处理
        
        private void HandlePlayerInputTick(uint inputTick, float deltaTime)
        {
            InputState input = _localInputProvider.GetInputState();
            _clientHandler.SendPlayerInput(inputTick, input);
        }

        #endregion

        #region 客户端消息处理

        public void HandleWorldSnapshot()
        {
            
        }

        #endregion
    }
}
