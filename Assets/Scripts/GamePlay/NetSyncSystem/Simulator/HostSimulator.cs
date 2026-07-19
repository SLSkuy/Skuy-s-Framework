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
        private readonly NetClient _client;
        private readonly TickSystem _simulateTickSystem;
        private readonly TickSystem _snapshotTickSystem;
        
        private readonly Dictionary<uint, NetPlayerCharacter> _players = new();
        private readonly Dictionary<uint, NetInputProvider> _inputProviders = new();
        private readonly Dictionary<uint, NetPlayerController> _controllers = new();
        private IInputStateProvider _localInput;

        public HostSimulator(NetClient client, NetServer server, SyncConfig config)
        {
            _server = server;
            _client = client;
            _simulateTickSystem = new TickSystem(Mathf.Max(0, config.simulationTickRate));
            _snapshotTickSystem = new TickSystem(Mathf.Max(0, config.snapshotTickRate));
        }

        public void Start()
        {
            if (IsRunning) return;

            _simulateTickSystem.OnTick += Simulate;
            _snapshotTickSystem.OnTick += BroadCastSnapShot;
            _simulateTickSystem.Start();
            _snapshotTickSystem.Start();
        }

        public void Stop()
        {
            if(!IsRunning) return;
            
            _simulateTickSystem.OnTick -= Simulate;
            _snapshotTickSystem.OnTick -= BroadCastSnapShot;
            _simulateTickSystem.Stop();
            _snapshotTickSystem.Stop();
        }

        /// <summary>
        /// 注册本地玩家
        /// </summary>
        /// <param name="localInput"></param>
        /// <param name="player"></param>
        public void RegisterLocalPlayer(IInputStateProvider localInput, NetPlayerCharacter player)
        {
            player.SetRole(NetEntityRole.Authority);
            _localInput = localInput;
            if (!_players.TryAdd(_client.ClientId, player))
            {
                _players[_client.ClientId] = player;
                Debug.LogWarning("[HostSimulator] Client " + _client.ClientId + " 已被注册，旧的将被强制覆盖");
            }
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

        #region 模拟管理

        /// <summary>
        /// 接收远端的输入状态变更
        /// </summary>
        public void UpdateInputState(uint clientId, Player_Input input)
        {
            if (!IsRunning || input == null) return;

            if(_inputProviders.TryGetValue(clientId, out NetInputProvider inputProvider))
            {
                inputProvider.SetInputState(NetSyncUtils.ToInputState(input));
            }
        }

        private void Simulate(uint tick)
        {
            foreach (var controller in _controllers.Values)
            {
                controller.Simulate(_simulateTickSystem.TickDeltaTime);
            }
        }
        
        private void BroadCastSnapShot(uint tick)
        {
            // 收集全局状态
            World_Snapshot snapshot = new World_Snapshot
            {
                Tick = tick
            };
            
            foreach (var player in _players)
            {
                snapshot.PlayerSnapshots.Add(
                    NetSyncUtils.ToPlayerSnapshot(player.Key, player.Value.GetSnapshot()));
            }

            foreach (var controller in _controllers)
            {
                _server.Send(controller.Key, NetEvent.WORLD_SNAPSHOT, snapshot);
            }
        }

        #endregion
    }
}
