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
    /// <summary>
    /// 客户端模拟控制器，收集本地输入命令并插值渲染权威快照
    /// </summary>
    public class ClientSimulator
    {
        public bool IsRunning => _tickSystem.IsRunning;
        
        private readonly NetClient _client;
        private readonly TickSystem _tickSystem;
        
        private readonly Dictionary<uint, NetPlayerCharacter> _players = new();
        private IInputStateProvider _localInput;
        
        public ClientSimulator(NetClient client, SyncConfig config)
        {
            _client = client;
            _tickSystem = new TickSystem(Mathf.Max(0, config.commandTickRate));
        }

        public void Start()
        {
            if (IsRunning) return;

            _tickSystem.OnTick += TickInputState;
            _tickSystem.Start();
        }

        public void Stop()
        {
            if(!IsRunning) return;
            
            _tickSystem.OnTick -= TickInputState;
            _tickSystem.Stop();
        }

        /// <summary>
        /// 注册本地玩家，后续使用LocalController进行预测，当前只接收网络快照进行本地同步
        /// </summary>
        public void RegisterLocalPlayer(IInputStateProvider localInput, NetPlayerCharacter player)
        {
            player.SetRole(NetEntityRole.Replica);
            _localInput = localInput;
            if (!_players.TryAdd(_client.ClientId, player))
            {
                _players[_client.ClientId] = player;
                Debug.LogWarning("[ClientSimulator] Client " + _client.ClientId + " 已被注册，旧的将被强制覆盖");
            }
        }

        /// <summary>
        /// 注册远程玩家
        /// </summary>
        public void RegisterRemotePlayer(uint clientId, NetPlayerCharacter player)
        {
            player.SetRole(NetEntityRole.Replica);
            if (!_players.TryAdd(clientId, player))
            {
                Debug.LogWarning("[ClientSimulator] Client " + clientId + " has already been registered");
            }
        }
        
        #region 同步管理
        
        /// <summary>
        /// 接收实体快照状态，由实体内部转换渲染Tick消费快照
        /// </summary>
        public void AddSnapshot(World_Snapshot snapshot)
        {
            if (!IsRunning || snapshot == null) return;

            foreach (var snap in snapshot.PlayerSnapshots)
            {
                if (_players.TryGetValue(snap.ClientId, out NetPlayerCharacter player))
                {
                    player.AddSnapshot(NetSyncUtils.ToPlayerSnapshot(snap));
                }
            }
        }

        /// <summary>
        /// 发送当前Tick对应的输入状态
        /// </summary>
        private void TickInputState(uint tick)
        {
            if (_localInput == null || _client.ClientId == 0 || !_client.HasFastChannel) return;

            InputState state = _localInput.GetInputState();
            Player_Input input = NetSyncUtils.ToPlayerInput(_client.ClientId, tick, state);
            _client.Send(NetEvent.PLAYER_INPUT, input);
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            _tickSystem.Update(deltaTime);
        }

        #endregion
    }
}