using Framework;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端模拟核：沿用模拟核节拍上传本机输入，并直接写入主机发来的世界快照。
    /// </summary>
    public sealed class ClientSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private IInputStateProvider _localInputProvider;
        private SimulationClientHandler _clientHandler;
        private Simulator _simulator;
        
        // 记录已经应用的最新的快照状态，丢弃过时的快照
        private uint _appliedSnapshotTick;

        #region 属性
        public override int Priority => 500;
        public Simulator SimulationKernal => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            
            _localInputProvider = Global.Get<LocalInputManager>().Provider;
            
            _clientHandler = new SimulationClientHandler(this);
            _clientHandler.Bind();
            
            _simulator.TickPlayerInput += OnPlayerInputTick;
            _simulator.StartClock();
            
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.TickPlayerInput -= OnPlayerInputTick;
            _simulator.StopClock();
            
            _clientHandler.Unbind();
            _clientHandler = null;
            _appliedSnapshotTick = 0;
            
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
        
        private void OnPlayerInputTick(uint inputTick, float deltaTime)
        {
            InputState input = _localInputProvider.GetInputState();
            _clientHandler.SendPlayerInput(inputTick, input);
        }

        #endregion

        #region 客户端消息处理

        /// <summary>
        /// 收到快照后立即把每个实体写成这份快照。
        /// </summary>
        public void HandleWorldSnapshot(World_Snapshot snapshot)
        {
            if (snapshot.SnapshotTick <= _appliedSnapshotTick) return;

            _appliedSnapshotTick = snapshot.SnapshotTick;
            foreach (var playerState in snapshot.PlayerSnapshots)
            {
                _simulator.TryRestoreEntityState(playerState.EntityId, ProtoUtils.ToRollbackState(playerState));
            }
        }

        #endregion
    }
}
