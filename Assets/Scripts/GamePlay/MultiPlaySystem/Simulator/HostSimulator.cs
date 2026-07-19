using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySystem;
using NetSync;
using Network;
using UnityEngine;
using Utils;

namespace GamePlay.NetSync
{
    public class HostSimulator
    {
        public bool IsRunning => _simulateTickSystem.IsRunning && _snapshotTickSystem.IsRunning;

        private readonly NetServer _server;
        private readonly TickSystem _simulateTickSystem;
        private readonly TickSystem _snapshotTickSystem;
        private readonly Dictionary<uint, NetPlayerCharacter> _players = new();
        private readonly Dictionary<uint, NetInputProvider> _inputProviders = new();
        private readonly Dictionary<uint, NetPlayerController> _controllers = new();

        public HostSimulator(NetServer server, SyncConfig config)
        {
            _server = server;
            _simulateTickSystem = new TickSystem(Mathf.Max(0, config.simulationTickRate));
            _snapshotTickSystem = new TickSystem(Mathf.Max(0, config.snapshotTickRate));
        }

        public void Start()
        {
            if (IsRunning) return;

            _simulateTickSystem.OnTick += Simulate;
            _snapshotTickSystem.OnTick += BroadcastSnapshot;
            _simulateTickSystem.Start();
            _snapshotTickSystem.Start();
        }

        public void Stop()
        {
            if(!IsRunning) return;
            
            _simulateTickSystem.OnTick -= Simulate;
            _snapshotTickSystem.OnTick -= BroadcastSnapshot;
            _simulateTickSystem.Stop();
            _snapshotTickSystem.Stop();
        }
        
        /// <summary>
        /// 注册远程玩家
        /// </summary>
        public void RegisterPlayer(uint clientId, NetPlayerCharacter player)
        {
            if (_players.TryAdd(clientId, player))
            {
                // 初始化网络控制器
                player.SetRole(NetEntityRole.Authority);
                var inputProvider = player.gameObject.AddComponent<NetInputProvider>();
                var controller = player.gameObject.AddComponent<NetPlayerController>();
                controller.Configure(player, inputProvider);
                
                _controllers.Add(clientId, controller);
                _inputProviders.Add(clientId, inputProvider);

                return;
            }
            
            Debug.LogWarning("[HostSimulator] Client " + clientId + " has already been registered");
        }

        /// <summary>
        /// 注销玩家
        /// </summary>
        public void UnregisterPlayer(uint clientId)
        {
            _players.Remove(clientId);
            _controllers.Remove(clientId);
            _inputProviders.Remove(clientId);
        }

        public void ClearPlayers()
        {
            _players.Clear();
            _controllers.Clear();
            _inputProviders.Clear();
        }

        #region 模拟管理

        /// <summary>
        /// 接收远端的输入状态变更
        /// </summary>
        public void UpdateInputState(uint clientId, Player_Input input)
        {
            if (!IsRunning || input == null) return;

            if (_inputProviders.TryGetValue(clientId, out NetInputProvider inputProvider))
            {
                inputProvider.SetInputState(NetSyncUtils.ToInputState(input));
            }
        }

        private void Simulate(uint tick)
        {
            foreach (NetPlayerController controller in _controllers.Values)
            {
                controller.Simulate(_simulateTickSystem.TickDeltaTime);
            }
        }

        private void BroadcastSnapshot(uint tick)
        {
            if (_players.Count == 0) return;

            World_Snapshot snapshot = new World_Snapshot { Tick = tick };
            foreach (KeyValuePair<uint, NetPlayerCharacter> player in _players)
            {
                Player_Snapshot playerSnapshot = NetSyncUtils.ToPlayerSnapshot(player.Key, player.Value.GetSnapshot());
                playerSnapshot.Tick = tick;
                snapshot.PlayerSnapshots.Add(playerSnapshot);
            }

            _server.Broadcast(NetEvent.WORLD_SNAPSHOT, snapshot);
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _simulateTickSystem.Update(deltaTime);
            _snapshotTickSystem.Update(deltaTime);
        }

        #endregion
    }
}
