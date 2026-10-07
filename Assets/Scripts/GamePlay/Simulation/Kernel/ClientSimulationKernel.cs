using Framework;
using GamePlay.EntitySystem;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端模拟核：沿用模拟核节拍上传本机输入，并把主机快照交给远端插值。
    /// </summary>
    public sealed class ClientSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private IInputStateProvider _localInputProvider;
        private SimulationClientHandler _clientHandler;
        private Simulator _simulator;
        
        private AuthorityFrameInterpolation _interpolation;
        private ClientFramePrediction _prediction;
        
        // 记录已经收到的最新快照序号，丢弃过时的快照
        private uint _latestSnapshotTick;

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
            
            _interpolation.Clear();
            _latestSnapshotTick = 0;
            
            IsSessionRunning = false;
        }
        
        #region 子系统生命周期

        public override void Init()
        {
            _simulator = new Simulator();
            _simulator.Init();
            _interpolation = new AuthorityFrameInterpolation(_simulator, _simulator.InterpolationDelayTicks, _simulator.SnapshotTickRate);
            _prediction = new ClientFramePrediction(_simulator.MaxFutureInputTicks, _simulator);
        }

        public override void Update(float deltaTime)
        {
            _simulator.Update(deltaTime);
            _interpolation.Present(deltaTime);
        }

        public override void Destroy()
        {
            StopSession();
            _interpolation.Clear();
            _interpolation = null;
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
        /// 收到快照后按实体写入权威帧缓冲，画面由插值模块逐帧推进。
        /// </summary>
        public void HandleWorldSnapshot(World_Snapshot snapshot)
        {
            if (snapshot.SnapshotTick <= _latestSnapshotTick) return;

            // 更新已处理的快照Tick
            _latestSnapshotTick = snapshot.SnapshotTick;
            foreach (var playerState in snapshot.PlayerSnapshots)
            {
                
                if (!_simulator.TryGet(playerState.EntityId, out EntityObjectIdentity identity, out EntityCharacter character)) continue;
                if (!identity || !character.IsInitialized) continue;

                // 只有 Replica 的实体进行插值处理
                if (identity.IsReplica)
                {
                    _interpolation.AddFrame(playerState.EntityId, new EntityAuthorityFrame
                    {
                        snapshotTick = snapshot.SnapshotTick,
                        state = ProtoUtils.ToRollbackState(playerState),
                    });
                }

                // 记录权威状态，进行预测和解
                if (identity.IsPredict)
                {
                    // TODO: 预测和解
                }
            }
        }

        #endregion
    }
}
