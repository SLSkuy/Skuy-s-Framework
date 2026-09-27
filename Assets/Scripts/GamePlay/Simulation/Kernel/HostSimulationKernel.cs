using System.Collections.Generic;
using Framework;
using GamePlay.Room;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 主机权威模拟核：持有 Simulator，以及远端玩家的输入来源。
    /// </summary>
    public sealed class HostSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private readonly Dictionary<uint, AuthorityInputProvider> _remoteInputs = new();
        private readonly List<PlayerProcessedSnapshot> _snapshotBuffer = new();
        private SimulationServerHandler _serverHandler;
        private Simulator _simulator;
        private RoomManager _room;

        #region 属性
        public override int Priority => 500;
        public Simulator SimulationKernal => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;

            _room = Global.Get<RoomManager>();
            if (_room.AcceptsRemoteJoin)
            {
                _simulator.TickSnapshot += OnSnapshotTick;
                _simulator.StartSnapshotClock();
                
                _serverHandler = new SimulationServerHandler(this);
                _serverHandler.Bind();
            }

            _simulator.StartClock();
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            if (_serverHandler != null)
            {
                _simulator.TickSnapshot -= OnSnapshotTick;
                _simulator.StopSnapshotClock();
                
                _serverHandler.Unbind();
                _serverHandler = null;
            }

            _remoteInputs.Clear();
            _simulator.StopClock();
            IsSessionRunning = false;
        }

        /// <summary>
        /// 记下远端玩家的输入来源，供随后到达的玩家输入入队。
        /// </summary>
        public void RegisterRemoteInput(uint playerId, AuthorityInputProvider provider)
        {
            _remoteInputs[playerId] = provider;
        }

        /// <summary>
        /// 远端玩家离开后不再接收其玩家输入。
        /// </summary>
        public void UnregisterRemoteInput(uint playerId)
        {
            _remoteInputs.Remove(playerId);
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

        #region 事件回调

        /// <summary>
        /// 采集世界状态，广播给所有客户端快照
        /// </summary>
        private void OnSnapshotTick(uint snapshotTick, float deltaTime)
        {
            _simulator.CaptureEntities(_snapshotBuffer);
            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                _snapshotBuffer[i] = _snapshotBuffer[i];
            }

            _serverHandler.BroadcastWorldSnapshot(snapshotTick, _snapshotBuffer);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(uint playerId, Player_Input input)
        {
            if (!_remoteInputs.TryGetValue(playerId, out AuthorityInputProvider provider)) return;

            provider.Enqueue(input.InputTick, ProtoUtils.ToInputState(input));
        }

        #endregion
    }
}
