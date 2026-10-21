using System;
using System.Collections.Generic;
using Framework;
using GamePlay.BattleSystem;
using GamePlay.DataProxy;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 主机权威模拟核：持有 Simulator，以及远端玩家的输入来源。
    /// </summary>
    public sealed class HostSimulationKernel : ISimulationKernel
    {
        private readonly RoomDataProxy _roomData = Global.GetDataProxy<RoomDataProxy>();
        
        private readonly Dictionary<Guid, AuthorityInputProvider> _remoteInputs = new();
        private readonly List<PlayerProcessedFrame> _snapshotBuffer = new();
        private BattleServerHandler _serverHandler;

        #region 属性
        public bool IsSessionRunning => Kernel.IsRunning;
        public Simulator Kernel { get; private set; }
        #endregion

        public HostSimulationKernel(BattleServerHandler handler)
        {
            _serverHandler = handler;
            Kernel = new Simulator();
        }

        public bool StartSession()
        {
            if (IsSessionRunning) return true;

            if (_roomData.AcceptRemoteJoin)
            {
                Kernel.SnapshotTick += OnSnapshotTick;
            }

            Kernel.StartServerTick();
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            if (_roomData.AcceptRemoteJoin)
            {
                Kernel.SnapshotTick -= OnSnapshotTick;
            }

            _remoteInputs.Clear();
            Kernel.StopTick();
        }

        /// <summary>
        /// 记下远端玩家的输入来源，供随后到达的玩家输入入队。
        /// </summary>
        public void RegisterRemoteInput(Guid playerId, AuthorityInputProvider provider)
        {
            _remoteInputs[playerId] = provider;
        }

        /// <summary>
        /// 远端玩家离开后不再接收其玩家输入。
        /// </summary>
        public void UnregisterRemoteInput(Guid playerId)
        {
            _remoteInputs.Remove(playerId);
        }

        #region 子系统生命周期

        public void Update(float deltaTime)
        {
            Kernel.Update(deltaTime);
        }

        public void Dispose()
        {
            StopSession();

            _serverHandler = null;
            
            Kernel.Dispose();
            Kernel = null;
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 采集世界状态，广播给所有客户端快照
        /// </summary>
        private void OnSnapshotTick(uint snapshotTick, float deltaTime)
        {
            _snapshotBuffer.Clear();
            foreach (KeyValuePair<uint, SimulationEntity> pair in Kernel.Entities)
            {
                SimulationEntity entity = pair.Value;
                if (!entity.Character || !entity.Character.IsInitialized) continue;

                _snapshotBuffer.Add(new PlayerProcessedFrame
                {
                    entityId = pair.Key,
                    playerId = entity.Identity.PlayerID,
                    lastProcessedInputTick = entity.GetLastProcessInputTick(),
                    snapshot = entity.Character.CaptureSnapshot(),
                });
            }
            _serverHandler.BroadcastWorldSnapshot(snapshotTick, _snapshotBuffer);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(Guid playerId, Player_Input input)
        {
            if (!_remoteInputs.TryGetValue(playerId, out AuthorityInputProvider provider)) return;

            if (input.RecentInputs.Count > 0)
            {
                for (int i = 0; i < input.RecentInputs.Count; i++)
                {
                    Input_Sample sample = input.RecentInputs[i];
                    provider.Enqueue(sample.InputTick, ProtoUtils.ToInputState(sample));
                }

                return;
            }

            provider.Enqueue(input.InputTick, ProtoUtils.ToInputState(input));
        }

        #endregion
    }
}
