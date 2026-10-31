using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.DataProxy;
using GamePlay.GameData;
using Google.Protobuf;
using NetConnect;
using NetSync;
using Network;
using UnityEngine;
using Utils;

namespace GamePlay.Room
{
    /// <summary>
    /// 房间对局管理器，对接存档、玩家管理，对业务层屏蔽网络连接数据
    /// </summary>
    public class RoomManager : SubSystemBase
    {
        private PlayerInfoDataProxy _playerData;
        private RoomDataProxy _roomData;
        
        private PlayerManager _playerManager;
        
        private readonly Dictionary<(NetEvent EventId, Delegate Callback), Delegate> _playerHandlers = new();
        private RoomClientHandler _clientHandler;
        private RoomServerHandler _serverHandler;
        private NetServer _netServer;
        private NetClient _netClient;
        
        #region 属性
        public override int Priority => (int)GamePlaySystemPriority.RoomManager;

        public SessionRole Role { get; private set; } = SessionRole.None;
        public RoomConfig Config => _roomData.CurrentConfig;
        public bool IsInMatch { get; private set; }
        public bool IsHost => Role == SessionRole.Host;
        public bool IsClient => Role == SessionRole.Client;
        #endregion

        #region 事件
        public event Action SessionConnected;   // 游戏会话连接成功 
        public event Action SessionDisconnected;   // 游戏会话断开连接 
        public event Action SessionReconnectFailed; // 游戏会话重连失败
        public event Action<Guid> PlayerJoined; // 玩家加入
        public event Action<Guid> PlayerLeft;   // 玩家退出
        #endregion
        
        #region 战局启动管理
        
        public void StartServer(RoomConfig config)
        {
            // 记录主机本地玩家数据
            PlayerJoinServer(GlobalConst.LOCAL_CONNECTION_ID, _playerData.LocalPlayerInfoData);
            _roomData.UpdateConfig(config);
            
            if (_roomData.AcceptRemoteJoin) StartListening();
            
            Role = SessionRole.Host;
            IsInMatch = true;
        }

        /// <summary>
        /// 开启客户端连接，当远端连接被接受时触发
        /// </summary>
        private void StartClient(Room_Join_Response response)
        {
            // 使用服务端返回的玩家数据
            foreach (var player in response.Remotes) PlayerJoinClient(player.ToData());
            _roomData.UpdateConfig(response.RoomCfg.ToData());
            
            Role = SessionRole.Client;
            IsInMatch = true;
        }
        
        /// <summary>
        /// 房间退出/销毁
        /// </summary>
        public void Dissolve()
        {
            if (!IsInMatch) return;
            
            // 房主销毁房间，广播销毁房间通知
            _serverHandler?.BroadcastHostDissolved();
            StopClient();
            StopServer();
            
            _playerManager.Dispose();
            
            IsInMatch = false;
        }
        
        #endregion

        #region 连接管理

        /// <summary>
        /// 开启服务端监听，不开启时就为本地游戏
        /// </summary>
        private void StartListening()
        {
            _netServer = Global.Register<NetServer>();
            _serverHandler = Global.RegisterHandler<RoomServerHandler>();
            _serverHandler.JoinRequested += HandlePlayerJoinRequest;
            _serverHandler.LeaveRequested += HandlePlayerLeaveRequest;
            _serverHandler.ClientRemoved += HandlePlayerUnexpectedLeave;
            _netServer.StartServer();
        }

        public void StartConnecting()
        {
            // 防止重复创建，重新连接连接时复用已经创建的配置
            if (!Global.TryGet(out _netClient)) _netClient = Global.Register<NetClient>();
            _netClient.OnReconnectFailed += OnReconnectFailed;
            if (_clientHandler == null)
            {
                _clientHandler = Global.RegisterHandler<RoomClientHandler>();
                _clientHandler.JoinResponseReceived += HandleJoinResponse;
                _clientHandler.PlayerJoinReceived += HandlePlayerJoinNotify;
                _clientHandler.PlayerLeaveReceived += HandlePlayerLeaveNotify;
                _clientHandler.ServerDissolved += HandleServerDissolveNotify;
                _clientHandler.ReliableConnectReceived += HandleReliableConnectResponse;
            }
            _netClient.StartReliableConnect();
        }

        private void StopConnecting()
        {
            _netClient.OnReconnectFailed -= OnReconnectFailed;
            _netClient.StopClient();
            _clientHandler.Unbind();
        }

        private void StopClient()
        {
            if (_netClient == null) return;
            
            // 发送退出通知
            // TODO: 当前发送只是入队，并没有直接发送，夏旭注销会直接导致消息没有发送出去，还在缓冲队列中
            if (IsInMatch) _clientHandler.SendGameLeaveRequest();

            StopConnecting();
            Global.Unregister<NetClient>();
            
            _clientHandler.JoinResponseReceived -= HandleJoinResponse;
            _clientHandler.PlayerJoinReceived -= HandlePlayerJoinNotify;
            _clientHandler.PlayerLeaveReceived -= HandlePlayerLeaveNotify;
            _clientHandler.ServerDissolved -= HandleServerDissolveNotify;
            _clientHandler.ReliableConnectReceived -= HandleReliableConnectResponse;
            Global.UnregisterHandler<RoomClientHandler>();
            
            _clientHandler = null;
            _netClient = null;
        }

        private void StopServer()
        {
            if (_netServer == null) return;
            
            _serverHandler.JoinRequested -= HandlePlayerJoinRequest;
            _serverHandler.LeaveRequested -= HandlePlayerLeaveRequest;
            _serverHandler.ClientRemoved -= HandlePlayerUnexpectedLeave;
            Global.UnregisterHandler<RoomServerHandler>();
            
            _playerHandlers.Clear();
            _netServer.StopServer();
            Global.Unregister<NetServer>();
            
            _serverHandler = null;
            _netServer = null;
        }

        #endregion

        #region 玩家管理

        public void PlayerJoinClient(PlayerInfoData playerInfo)
        {
            _playerManager.AddPlayerClient(playerInfo);
            PlayerJoined?.Invoke(playerInfo.uuid);
        }

        public void PlayerJoinServer(uint connectionId, PlayerInfoData playerInfo)
        {
            _playerManager.AddPlayerServer(connectionId, playerInfo);
            PlayerJoined?.Invoke(playerInfo.uuid);
        }

        public void PlayerLeaveClient(Guid uuid)
        {
            _playerManager.RemovePlayerClient(uuid);
            PlayerLeft?.Invoke(uuid);
        }

        public Guid PlayerLeaveServer(uint connectionId)
        {
            Guid uuid = _playerManager.RemovePlayerServer(connectionId);
            PlayerLeft?.Invoke(uuid);
            return uuid;
        }

        /// <summary>
        /// 获取房间内的所有玩家UUID信息
        /// </summary>
        public PlayerInfoData[] GetAllPlayerData()
        {
            return _playerManager.GetActivePlayerData();
        }

        /// <summary>
        /// 获取房间内的所有玩家连接ID信息
        /// </summary>
        public uint[] GetAllPlayerConnectionID()
        {
            return _playerManager.GetActiveConnectionIds();
        }

        #endregion
        
        #region 子系统生命周期

        public override void Init()
        {
            _playerData = Global.GetDataProxy<PlayerInfoDataProxy>();
            _roomData = Global.GetDataProxy<RoomDataProxy>();
            _playerManager = new PlayerManager();
        }

        public override void Destroy()
        {
            Dissolve();
            StopClient();
            StopServer();
            
            _playerData = null;
            _roomData = null;
            _playerManager = null;
        }

        #endregion

        #region 事件回调

        private void OnReconnectFailed()
        {
            SessionReconnectFailed?.Invoke();
        }

        #endregion

        #region 客户端消息处理

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response response)
        {
            // TODO: 由独立模块处理连接，现临时使用
            _clientHandler.SendJoinRequest(_playerData.LocalPlayerInfoData);
        }

        private void HandleJoinResponse(Room_Join_Response response)
        {
            Debug.Log(response);
            
            if (response.Accepted)
            {
                StartClient(response);
                SessionConnected?.Invoke();
            }
            else
            {
                // TODO: 拒绝连接提示
                StopConnecting();   // 断开本次连接
            }
        }

        private void HandlePlayerJoinNotify(Room_Player_Joined_Notify notify)
        {
            PlayerJoinClient(notify.Player.ToData());
        }

        private void HandlePlayerLeaveNotify(Room_Player_Leave_Notify notify)
        {
            PlayerLeaveClient(Guid.Parse(notify.UUID));
        }

        private void HandleServerDissolveNotify(Room_Server_Dissolved_Notify notify)
        {
            Dissolve();
            SessionDisconnected?.Invoke();
        }

        #endregion

        #region 服务端消息处理

        private void HandlePlayerJoinRequest(uint connectionId, Room_Join_Request request)
        {
            PlayerInfoData playerInfo = request.Player.ToData();
            
            // 房间拒绝远端连接
            if(!_roomData.AcceptRemoteJoin)
            {
                _serverHandler.SendPlayerJoinResponse(connectionId, false, null);
                return;
            }
            
            // 房间玩家数量已达上限
            if(_playerManager.PlayerCount >= _roomData.MultiPlayCapacity)
            {
                _serverHandler.SendPlayerJoinResponse(connectionId, false, null);
                return;
            }
            
            // 判断是否在局内
            if (_playerManager.TryGetPlayer(playerInfo.uuid, out _))
            {
                // 玩家已经在局内，拒绝当前加入连接请求
                _serverHandler.SendPlayerJoinResponse(connectionId, false, null);
                return;
            }
            
            // 添加对局内玩家实例记录
            PlayerJoinServer(connectionId, playerInfo);
            _serverHandler.SendPlayerJoinResponse(connectionId, true, playerInfo);
        }

        /// <summary>
        /// 玩家退出游戏房间请求
        /// </summary>
        private void HandlePlayerLeaveRequest(uint connectionId, Room_Leave_Request request)
        {
            Guid uuid = PlayerLeaveServer(connectionId);
            _serverHandler.BroadcastPlayerLeaved(uuid.ToString());
            
            // TODO: NetServer删除Connection记录
        }

        /// <summary>
        /// 玩家异常退出游戏房间
        /// </summary>
        private void HandlePlayerUnexpectedLeave(uint connectionId)
        {
            Guid uuid = PlayerLeaveServer(connectionId);
            _serverHandler.BroadcastPlayerLeaved(uuid.ToString());
            
            // TODO: NetServer删除Connection记录
        }

        #endregion

        #region 连接重定向

        public void Send(Guid playerId, NetEvent evt, IMessage message)
        {
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                _netServer.Send(player.ConnectionID, evt, message);
            }
        }

        public void SendReliable(Guid playerId, NetEvent evt, IMessage message)
        {
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                _netServer.SendReliable(player.ConnectionID, evt, message);
            }
        }

        public void SendGroup(uint[] clientIds, NetEvent evt, IMessage message)
        {
            _netServer.SendGroup(clientIds, evt, message);
        }

        public void SendGroupWithout(Guid without, uint[] clientIds, NetEvent evt, IMessage message)
        {
            if (_playerManager.TryGetPlayer(without, out Player player))
            {
                _netServer.SendGroupWithout(player.ConnectionID, clientIds, evt, message);
            }
        }
        
        public void SendGroupReliable(uint[] clientIds, NetEvent evt, IMessage message)
        {
            _netServer.SendGroupReliable(clientIds, evt, message);
        }
        
        public void SendGroupReliableWithout(Guid without, uint[] clientIds, NetEvent evt, IMessage message)
        {
            if (_playerManager.TryGetPlayer(without, out Player player))
            {
                _netServer.SendGroupReliableWithout(player.ConnectionID, clientIds, evt, message);
            }
        }

        /// <summary>
        /// 登记服务端消息回调。入座后的连接号会先换成玩家号，再交给 callback
        /// 尚未入座的连接没有玩家号，不转发。没有开启监听时无法登记
        /// </summary>
        public void RegisterPlayerHandler<T>(NetEvent eventId, Action<Guid, T> callback)
            where T : class, IMessage, new()
        {
            if (callback == null || _netServer == null) return;

            (NetEvent EventId, Delegate Callback) key = (eventId, callback);
            if (_playerHandlers.ContainsKey(key)) return;

            Action<uint, T> adapter = (connectionId, message) =>
            {
                if (!_playerManager.TryGetPlayer(connectionId, out Player player)) return;
                callback(player.UUID, message);
            };

            _playerHandlers.Add(key, adapter);
            _netServer.RegisterHandler(eventId, adapter);
        }

        /// <summary>
        /// 拆除 RegisterPlayerHandler 登记的回调。
        /// </summary>
        public void UnregisterPlayerHandler<T>(NetEvent eventId, Action<Guid, T> callback)
            where T : class, IMessage, new()
        {
            if (!_playerHandlers.Remove((eventId, callback), out Delegate adapter)) return;
            if (_netServer != null) _netServer.UnregisterHandler(eventId, (Action<uint, T>)adapter);
        }

        #endregion
    }
}