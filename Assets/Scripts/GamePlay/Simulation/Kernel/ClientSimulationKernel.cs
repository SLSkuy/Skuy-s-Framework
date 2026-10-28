using Framework;
using GamePlay.BattleSystem;
using GamePlay.EntitySystem;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端模拟核：沿用模拟核节拍上传本机输入，并把主机快照交给远端插值。
    /// </summary>
    public class ClientSimulationKernel : ISimulationKernel
    {
        private BattleClientHandler _clientHandler;
        private IInputProvider _localInputProvider;

        private VisualInterpolation _visualInterpolation;
        private SnapshotInterpolation _snapshotInterpolation;
        private ClientPrediction _clientPrediction;
        
        // 记录已经收到的最新快照序号，丢弃过时的快照
        private uint _latestSnapshotTick;

        #region 属性
        public bool IsSessionRunning => Kernel.IsRunning;
        public Simulator Kernel { get; private set; }
        #endregion

        public ClientSimulationKernel(BattleClientHandler handler)
        {
            _clientHandler = handler;
            Kernel = new Simulator();
            
            _visualInterpolation = new VisualInterpolation(Kernel); // 视觉效果插值，逻辑位置保持固定Tick步进
            _snapshotInterpolation = new SnapshotInterpolation(Kernel); // 快照插值，插值权威快照间过渡
            _clientPrediction = new ClientPrediction(Kernel);   // 客户端预测
        }

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            
            _localInputProvider = Global.Get<LocalInputManager>().Provider;
            
            Kernel.CollectPlayerInput += OnPlayerInputTick;
            Kernel.LocalPredict += OnPredict;
            Kernel.StartClientTick();

            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            Kernel.CollectPlayerInput -= OnPlayerInputTick;
            Kernel.LocalPredict -= OnPredict;
            Kernel.StopTick();
            
            _snapshotInterpolation.Clear();
            _clientPrediction.Clear();
            _latestSnapshotTick = 0;
        }
        
        #region 子系统生命周期

        public void Update(float deltaTime)
        {
            Kernel.Update(deltaTime);
            _snapshotInterpolation.Present(deltaTime);
        }

        public void Dispose()
        {
            StopSession();
            
            _visualInterpolation.Dispose();
            _visualInterpolation = null;
            
            _snapshotInterpolation.Clear();
            _snapshotInterpolation = null;
            
            _clientPrediction.Clear();
            _clientPrediction = null;
            
            _clientHandler = null;
            
            Kernel.Dispose();
            Kernel = null;
        }

        #endregion

        #region 回调处理
        
        private void OnPlayerInputTick(uint inputTick)
        {
            InputCommand input = _localInputProvider.GetInputCommand();
            _clientHandler.SendPlayerInput(inputTick, input);
        }

        private void OnPredict(uint inputTick, float deltaTime)
        {
            _clientPrediction.Predict(inputTick);
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
                
                if (!Kernel.TryGet(playerState.EntityId, out EntityIdentity identity, out EntityCharacter character)) continue;
                if (!identity || !character.IsInitialized) continue;

                // 只有 Replica 的实体进行插值处理
                if (identity.IsReplica)
                {
                    _snapshotInterpolation.AddFrame(playerState.EntityId, new EntityAuthorityFrame
                    {
                        snapshotTick = snapshot.SnapshotTick,
                        state = playerState.ToSnapshot(),
                    });
                }

                // 记录权威状态，进行预测和解
                if (identity.IsPredict)
                {
                    _clientPrediction.ReceiveAuthorityFrame(playerState.EntityId, playerState.LastProcessedInputTick, new EntityAuthorityFrame
                    {
                        snapshotTick = snapshot.SnapshotTick,
                        state = playerState.ToSnapshot(),
                    });
                }
            }
        }

        #endregion
    }
}
