using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySystem;
using NetSync;
using Network;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 多人游戏控制器，管理多人游戏的生命周期，事件分发
    /// </summary>
    public sealed class MultiPlayManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetSyncManager;
        
        public float RTT => _client?.RTT ?? 0f;
        public uint LocalClientId => _client?.ClientId ?? 0;
        
        public bool IsHostRunning => _server?.IsRunning == true && _hostSimulator?.IsRunning == true;
        public bool IsClientRunning => _client?.IsRunning == true && _clientSimulator?.IsRunning == true;
        public int HostPlayerCount => _playerManager?.HostPlayerCount ?? 0;
        public int ClientPlayerCount => _playerManager?.ClientPlayerCount ?? 0;

        private NetClient _client;
        private NetServer _server;
        private ClientSimulator _clientSimulator;
        private HostSimulator _hostSimulator;
        private NetworkPlayerManager _playerManager;
        private readonly HashSet<uint> _activePlayers = new();
        
        /// <summary>
        /// 关闭Host时判断是否需要同步关闭Client
        /// </summary>
        private bool _ownsLocalClient;
        
        public void StartClient()
        {
            if (IsClientRunning) return;
            
            _clientSimulator.Start();
            _client.StartReliableConnect();
            
            Debug.Log($"[MultiPlayManager] Client started for {NetClientConfig.Instance.ip}.");
        }
        
        public void StartHost(bool connectLocalClient = true)
        {
            if (!IsHostRunning)
            {
                _server.StartServer();
                _hostSimulator.Start();
                Debug.Log("[MultiPlayManager] Host started.");
            }

            if (connectLocalClient && !IsClientRunning)
            {
                _ownsLocalClient = true;
                StartClient();
            }
        }

        public void StopClient()
        {
            _clientSimulator?.Stop();
            _clientSimulator?.ClearPlayers();
            _playerManager?.ClearClientPlayers();
            _client?.StopClient();
            
            _ownsLocalClient = false;
        }

        public void StopHost()
        {
            bool stopOwnedClient = _ownsLocalClient;
            _hostSimulator?.Stop();
            _hostSimulator?.ClearPlayers();
            _playerManager?.ClearHostPlayers();
            _server?.StopServer();

            if (stopOwnedClient) StopClient();
        }

        public void StopAll()
        {
            StopClient();
            StopHost();
        }

        /// <summary>
        /// 发送加入游戏请求
        /// </summary>
        public bool SendJoinRequest()
        {
            if (_client == null || !_client.IsRunning || _client.ClientId == 0) return false;
            
            _client.SendReliable(NetEvent.GAME_JOIN_REQUEST, new Game_Join_Request
            {
                ClientId = _client.ClientId
            });
            Debug.Log($"[MultiPlayManager] Sent GAME_JOIN_REQUEST for client {_client.ClientId}.");
            return true;
        }
        
        #region 事件回调

        private void HandleGameJoinRequest(uint clientId, Game_Join_Request request)
        {
            bool accepted = request != null && request.ClientId == clientId && clientId != 0;
            if (accepted)
            {
                NetPlayerCharacter player = _playerManager.GetOrCreateHostPlayer(clientId, out bool created);
                accepted = player != null;
                if (accepted && created) _hostSimulator.RegisterPlayer(clientId, player);
            }

            _server.SendReliable(clientId, NetEvent.GAME_JOIN_RESPONSE, new Game_Join_Response
            {
                Accepted = accepted
            });
        }

        private void HandleGameJoinResponse(Game_Join_Response response)
        {
            bool accept = response?.Accepted == true;
            if (!accept)
            {
                Debug.LogWarning("[MultiPlayManager] Join Game was rejected.");
                return;
            }

            NetPlayerCharacter player = _playerManager.GetOrCreateClientPlayer(_client.ClientId, out _);
            IInputStateProvider input = _playerManager.EnsureLocalInput(player);
            _clientSimulator.RegisterLocalPlayer(input, player);
        }

        private void HandlePlayerInput(uint clientId, Player_Input input)
        {
            if (input == null || input.ClientId != clientId) return;
            _hostSimulator.UpdateInputState(clientId, input);
        }

        private void HandleWorldSnapshot(World_Snapshot snapshot)
        {
            if (snapshot == null || !IsClientRunning) return;

            // 判断是否有失活的玩家
            foreach (Player_Snapshot playerSnapshot in snapshot.PlayerSnapshots)
            {
                uint clientId = playerSnapshot.ClientId;
                if (clientId == 0) continue;
                _activePlayers.Add(clientId);

                NetPlayerCharacter player = _playerManager.GetOrCreateClientPlayer(clientId, out bool created);
                if (!player) continue;

                // 判断是否有新的玩家加入，加入模拟列表
                if (clientId == _client.ClientId)
                {
                    if (created)
                    {
                        IInputStateProvider input = _playerManager.EnsureLocalInput(player);
                        _clientSimulator.RegisterLocalPlayer(input, player);
                    }
                }
                else if (created)
                {
                    _clientSimulator.RegisterRemotePlayer(clientId, player);
                }
            }

            _clientSimulator.AddSnapshot(snapshot);
            _playerManager.RemoveClientPlayersExcept(_activePlayers);
            _activePlayers.Clear();
        }

        private void HandleClientRemoved(uint clientId)
        {
            _hostSimulator.UnregisterPlayer(clientId);
            _playerManager.RemoveHostPlayer(clientId);
            Debug.Log($"[MultiPlayManager] Removed expired client player {clientId}.");
        }

        #endregion
        
        #region 生命周期

        public override void Init()
        {
            SystemManager systems = Global.Get<SystemManager>();
            if (systems == null)
            {
                Debug.LogError("[MultiPlayManager] SystemManager is not available.");
                return;
            }

            _server = systems.GetSystem<NetServer>() ?? systems.RegisterSystem<NetServer>();
            _client = systems.GetSystem<NetClient>() ?? systems.RegisterSystem<NetClient>();
            _playerManager = new NetworkPlayerManager();
            _hostSimulator = new HostSimulator(_server, SyncConfig.Instance);
            _clientSimulator = new ClientSimulator(_client, SyncConfig.Instance);
            _server.OnClientRemoved += HandleClientRemoved;
        }

        public override void Update(float deltaTime)
        {
            if (_hostSimulator?.IsRunning == true) _hostSimulator.Update(deltaTime);
            if (_clientSimulator?.IsRunning == true) _clientSimulator.Update(deltaTime);
        }

        public override void BindEvents()
        {
            _server.RegNetHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleGameJoinRequest);
            _client.RegNetHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
            _server.RegNetHandler<Player_Input>(NetEvent.PLAYER_INPUT, HandlePlayerInput);
            _client.RegNetHandler<World_Snapshot>(NetEvent.WORLD_SNAPSHOT, HandleWorldSnapshot);
        }

        public override void Destroy()
        {
            StopAll();

            if (_server != null)
            {
                _server.OnClientRemoved -= HandleClientRemoved;
                _server.UnRegNetHandler(NetEvent.GAME_JOIN_REQUEST);
                _server.UnRegNetHandler(NetEvent.PLAYER_INPUT);
            }

            if (_client != null)
            {
                _client.UnRegNetHandler(NetEvent.GAME_JOIN_RESPONSE);
                _client.UnRegNetHandler(NetEvent.PLAYER_SNAPSHOT);
                _client.UnRegNetHandler(NetEvent.WORLD_SNAPSHOT);
            }

            _playerManager?.Dispose();
            _playerManager = null;
            _hostSimulator = null;
            _clientSimulator = null;
        }

        #endregion
    }
}
