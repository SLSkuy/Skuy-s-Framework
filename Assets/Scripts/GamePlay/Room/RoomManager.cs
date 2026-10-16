using System;
using Framework;
using GamePlay.DataProxy;
using GamePlay.GameSave;
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
        private PlayerDataProxy _playerData;
        private RoomDataProxy _roomData;
        
        private PlayerManager _playerManager;
        
        private RoomClientHandler _clientHandler;
        private RoomServerHandler _serverHandler;
        private NetServer _netServer;
        private NetClient _netClient;
        
        #region 属性
        public override int Priority => (int)GamePlaySystemPriority.RoomManager;

        public SessionRole Role { get; private set; } = SessionRole.None;
        public RoomConfig Config => _roomData.CurrentConfig;
        public bool IsInMatch { get; private set; }
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
            PlayerJoinServer(GlobalConst.LOCAL_CONNECTION_ID, _playerData.LocalPlayerData);
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
            _serverHandler = new RoomServerHandler(this);
            _serverHandler.Bind();
            _netServer.StartServer();
        }

        public void StartConnecting()
        {
            // 防止重复创建，重新连接连接时复用已经创建的配置
            if (!Global.TryGet(out _netClient)) _netClient = Global.Register<NetClient>();
            _netClient.OnReconnectFailed += OnReconnectFailed;
            _clientHandler ??= new RoomClientHandler(this);
            _clientHandler.Bind();
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
            
            _clientHandler = null;
            _netClient = null;
        }

        private void StopServer()
        {
            if (_netServer == null) return;
            
            _serverHandler.Unbind();
            _netServer.StopServer();
            Global.Unregister<NetServer>();
            
            _serverHandler = null;
            _netServer = null;
        }

        #endregion

        #region 玩家管理

        public void PlayerJoinClient(PlayerData player)
        {
            _playerManager.AddPlayerClient(player);
            PlayerJoined?.Invoke(player.uuid);
        }

        public void PlayerJoinServer(uint connectionId, PlayerData player)
        {
            _playerManager.AddPlayerServer(connectionId, player);
            PlayerJoined?.Invoke(player.uuid);
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
        public PlayerData[] GetAllPlayerData()
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
            _playerData = Global.GetDataProxy<PlayerDataProxy>();
            _roomData = Global.GetDataProxy<RoomDataProxy>();
            _playerManager = new PlayerManager();
        }

        public override void Destroy()
        {
            Dissolve();
            
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

        public void HandleReliableConnectResponse(Client_Reliable_Connect_Response response)
        {
            // TODO: 由独立模块处理连接，现临时使用
            _clientHandler.SendJoinRequest(_playerData.LocalPlayerData);
        }

        public void HandleJoinResponse(Room_Join_Response response)
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

        public void HandlePlayerJoinNotify(Room_Player_Joined_Notify notify)
        {
            PlayerJoinClient(notify.Player.ToData());
        }

        public void HandlePlayerLeaveNotify(Room_Player_Leave_Notify notify)
        {
            PlayerLeaveClient(Guid.Parse(notify.UUID));
        }

        public void HandleServerDissolveNotify(Room_Server_Dissolved_Notify notify)
        {
            Dissolve();
            SessionDisconnected?.Invoke();
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerJoinRequest(uint connectionId, Room_Join_Request request)
        {
            PlayerData player = request.Player.ToData();
            
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
            if (_playerManager.TryGetPlayer(player.uuid, out _))
            {
                // 玩家已经在局内，拒绝当前加入连接请求
                _serverHandler.SendPlayerJoinResponse(connectionId, false, null);
                return;
            }
            
            // 添加对局内玩家实例记录
            PlayerJoinServer(connectionId, player);
            _serverHandler.SendPlayerJoinResponse(connectionId, true, player);
        }

        /// <summary>
        /// 玩家退出游戏房间请求
        /// </summary>
        public void HandlePlayerLeaveRequest(uint connectionId, Room_Leave_Request request)
        {
            Guid uuid = PlayerLeaveServer(connectionId);
            _serverHandler.BroadcastPlayerLeaved(uuid.ToString());
            
            // TODO: NetServer删除Connection记录
        }

        /// <summary>
        /// 玩家异常退出游戏房间
        /// </summary>
        public void HandlePlayerUnexpectedLeave(uint connectionId)
        {
            Guid uuid = PlayerLeaveServer(connectionId);
            _serverHandler.BroadcastPlayerLeaved(uuid.ToString());
            
            // TODO: NetServer删除Connection记录
        }

        #endregion
    }
}