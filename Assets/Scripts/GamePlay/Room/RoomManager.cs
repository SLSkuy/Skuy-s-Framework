using System;
using System.Collections.Generic;
using Events;
using Framework;
using Google.Protobuf;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 对局名册：入座、开听与解散。已入座的消息按玩家号收发，连接只用于入座和断开。
    /// </summary>
    public sealed class RoomManager : SubSystemBase
    {
        private const int DEFAULT_CAPACITY = 4;
        private const int LOCAL_CAPACITY = 1;
        private const uint LOCAL_CONNECTION_ID = 0;

        private readonly Dictionary<uint, Player> _playersById = new();
        private readonly Dictionary<Delegate, Delegate> _playerHandlers = new();
        private Dictionary<uint, uint> _playerByConnection;
        private Dictionary<uint, uint> _connectionByPlayer;
        private uint _nextPlayerId = 1; // 玩家ID从1开始分配

        private RoomServerHandler _serverHandler;
        private RoomClientHandler _clientHandler;
        private NetServer _netServer;
        private NetClient _netClient;

        #region 属性
        public override int Priority => 150;
        
        // 房间状态
        public SessionRole SessionRole { get; private set; }
        public int Capacity { get; private set; }
        public int MemberCount => _playersById.Count;
        public bool IsInMatch { get; private set; }
        public bool AcceptsRemoteJoin { get; private set; }

        public uint HostPlayerId { get; private set; }
        public uint LocalPlayerId { get; private set; }
        #endregion

        #region 事件
        public event Action SessionEnded;   // 游戏会话结束
        public event Action<uint> PlayerJoined;  // 玩家加入
        public event Action<uint> PlayerRemoved;    // 玩家退出
        #endregion

        #region 对局管理

        /// <summary>
        /// 创建不接远端的对局，并让本机玩家入座。
        /// </summary>
        public void CreateLocalRoom()
        {
            OpenMatch(false);
        }

        /// <summary>
        /// 创建接远端的对局，本机入座后开启监听。
        /// </summary>
        public void CreateHostRoom()
        {
            if (!OpenMatch(true))
            {
                return;
            }

            StartListening();
        }

        /// <summary>
        /// 打开对局名册。本机玩与开多人共用；测试可只开名册而不启动监听。
        /// </summary>
        public bool OpenMatch(bool acceptsRemoteJoin, SessionRole sessionRole = SessionRole.Host)
        {
            if (IsInMatch)
            {
                return false;
            }

            AcceptsRemoteJoin = acceptsRemoteJoin;
            Capacity = acceptsRemoteJoin ? DEFAULT_CAPACITY : LOCAL_CAPACITY;
            SessionRole = sessionRole;

            uint playerId = _nextPlayerId;
            if (!TryAdmitPlayer(playerId))
            {
                return false;
            }

            _nextPlayerId++;
            BindConnection(LOCAL_CONNECTION_ID, playerId);
            LocalPlayerId = playerId;
            IsInMatch = true;
            return true;
        }

        /// <summary>
        /// 开启监听客户端连接
        /// </summary>
        private void StartListening()
        {
            _netServer = Global.Get<NetServer>();
            _serverHandler.Bind();
            _netServer.StartServer();
        }

        private void StopHost()
        {
            if (_netServer == null)
            {
                return;
            }

            _serverHandler.Unbind();
            _netServer = null;
        }

        private void StopClient()
        {
            if (_netClient == null)
            {
                return;
            }

            if (IsInMatch)
            {
                _clientHandler.SendGameLeaveRequest();
            }

            _clientHandler.Unbind();
            _netClient = null;
        }

        public uint[] GetPlayerIds()
        {
            uint[] playerIds = new uint[_playersById.Count];
            _playersById.Keys.CopyTo(playerIds, 0);
            return playerIds;
        }
        
        private bool TryGetPlayerId(uint connectionId, out uint playerId)
        {
            return _playerByConnection.TryGetValue(connectionId, out playerId);
        }
        
        /// <summary>
        /// 将连接加入当前对局。
        /// </summary>
        public uint Admit(uint connectionId)
        {
            if (!IsInMatch) return 0;
            if (connectionId != LOCAL_CONNECTION_ID && !AcceptsRemoteJoin) return 0;
            if (_playerByConnection.ContainsKey(connectionId)) return 0;
            if (_playersById.Count >= Capacity) return 0;
            
            uint playerId = _nextPlayerId;
            if (!TryAdmitPlayer(playerId)) return 0;

            _nextPlayerId++;
            BindConnection(connectionId, playerId);
            
            return playerId;
        }

        /// <summary>
        /// 连接离开。房主离开则解散对局。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (!IsInMatch) return;
            if (!UnbindConnection(connectionId, out uint playerId)) return;

            bool wasHost = playerId == HostPlayerId;
            if (wasHost)
            {
                Dissolve();
                return;
            }

            RemovePlayer(playerId);
            PlayerRemoved?.Invoke(playerId);
        }

        /// <summary>
        /// 处理房间关闭事件并通知其他玩家房间关闭
        /// </summary>
        private void Dissolve()
        {
            if (IsInMatch)
            {
                ClearMembers();
                IsInMatch = false;
            }

            if (_netServer != null)
            {
                // 房主销毁房间，广播销毁房间通知
                _serverHandler.BroadcastHostDissolved(new Room_Host_Dissolved_Notify());
            }

            StopHost();
            StopClient();
        }

        /// <summary>
        /// 尝试添加玩家
        /// 主机和客户端都走这套逻辑，客户端应用主机下发的玩家增量，按理说是不会出任何问题
        /// </summary>
        private bool TryAdmitPlayer(uint playerId)
        {
            if (_playersById.Count >= Capacity) return false;

            Player player = new(playerId);
            _playersById[playerId] = player;
            if (HostPlayerId == 0) HostPlayerId = playerId;

            PlayerJoined?.Invoke(playerId);
            return true;
        }

        private void RemovePlayer(uint playerId)
        {
            if (_playersById.Remove(playerId, out Player player))
            {
                player.State = PlayerState.Left;
            }
        }

        private void ClearMembers()
        {
            foreach (Player player in _playersById.Values)
            {
                player.State = PlayerState.Left;
            }

            _playersById.Clear();
            _playerByConnection.Clear();
            _connectionByPlayer.Clear();
            _nextPlayerId = 1;
            HostPlayerId = 0;
            LocalPlayerId = 0;
        }

        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            _nextPlayerId = 1;
            _playerByConnection = new Dictionary<uint, uint>();
            _connectionByPlayer = new Dictionary<uint, uint>();
            _serverHandler = new RoomServerHandler(this);
            _clientHandler = new RoomClientHandler(this);
        }

        public override void Destroy()
        {
            Dissolve();
        }

        #endregion

        #region 客户端消息处理
        
        /// <summary>
        /// 菜单加入被接受后，接管已有客户端连接并写入名册。
        /// </summary>
        public void HandleJoinResponse(Room_Join_Response response)
        {
            _netClient = Global.Get<NetClient>();
            _clientHandler.Bind();
            
            // 设置玩家数据
            _playersById.Clear();
            HostPlayerId = response.HostPlayerId;
            LocalPlayerId = response.PlayerId;
            foreach (uint playerId in response.PlayerIds)
            {
                TryAdmitPlayer(playerId);
            }
            
            // 设置房间属性
            SessionRole = SessionRole.Client;
            Capacity = DEFAULT_CAPACITY;
            AcceptsRemoteJoin = false;
            IsInMatch = true;
        }

        /// <summary>
        /// 应用主机下发的权威玩家增量通知
        /// </summary>
        public void HandlePlayerJoinedNotify(Room_Player_Joined_Notify notify)
        {
            uint playerId = notify.PlayerId;
            if (playerId == 0 || _playersById.ContainsKey(playerId)) return;    // 按理说这条return永远也不会触发
            TryAdmitPlayer(playerId);
        }

        /// <summary>
        /// 应用远端玩家离开通知
        /// </summary>
        public void HandlePlayerLeaveNotify(Room_Player_Leave_Notify notify)
        {
            if (!IsInMatch) return;

            RemovePlayer(notify.PlayerId);
            PlayerRemoved?.Invoke(notify.PlayerId);
        }

        /// <summary>
        /// 应用远端房主关闭房间通知
        /// </summary>
        public void HandleHostDissolvedNotify(Room_Host_Dissolved_Notify notify)
        {
            // 房主退出，销毁房间
            Dissolve();
            SessionEnded?.Invoke();
        }

        #endregion

        #region 服务端消息处理

        /// <summary>
        /// 按连接离座。未入座或对局已不存在时返回 0
        /// </summary>
        public uint HandlePlayerLeaveRequest(uint connectionId)
        {
            if (!IsInMatch || !TryGetPlayerId(connectionId, out uint playerId))
            {
                return 0;
            }

            Leave(connectionId);
            return playerId;
        }

        #endregion

        #region 网路注册重映射

        /// <summary>
        /// 登记已入座玩家的消息。回调收到的是玩家号；尚未入座的连接在这里被丢掉。
        /// </summary>
        public void RegisterPlayerHandler<T>(NetEvent eventId, Action<uint, T> callback) where T : class, IMessage, new()
        {
            if (_playerHandlers.ContainsKey(callback)) return;

            PlayerHandler<T> handler = new(this, callback);
            Action<uint, T> adapter = handler.Invoke;
            _playerHandlers.Add(callback, adapter);
            Global.Get<NetServer>().RegisterHandler(eventId, adapter);
        }

        /// <summary>
        /// 拆除已入座玩家的消息。须传入登记时的同一回调。
        /// </summary>
        public void UnregisterPlayerHandler<T>(NetEvent eventId, Action<uint, T> callback) where T : class, IMessage, new()
        {
            if (!_playerHandlers.Remove(callback, out Delegate adapter)) return;

            Global.Get<NetServer>().UnregisterHandler(eventId, (Action<uint, T>)adapter);
        }

        /// <summary>
        /// 按玩家号可靠发送
        /// </summary>
        public void SendReliable(uint playerId, NetEvent eventId, IMessage message)
        {
            _netServer.SendReliable(_connectionByPlayer[playerId], eventId, message);
        }

        /// <summary>
        /// 按玩家号发送
        /// </summary>
        public void Send(uint playerId, NetEvent eventId, IMessage message)
        {
            _netServer.Send(_connectionByPlayer[playerId], eventId, message);
        }

        public void BroadcastReliable(NetEvent eventId, IMessage message)
        {
            _netServer.BroadcastReliable(eventId, message);
        }

        public void Broadcast(NetEvent eventId, IMessage message)
        {
            _netServer.Broadcast(eventId, message);
        }
        
        private void BindConnection(uint connectionId, uint playerId)
        {
            _playerByConnection[connectionId] = playerId;
            _connectionByPlayer[playerId] = connectionId;
        }

        private bool UnbindConnection(uint connectionId, out uint playerId)
        {
            if (!_playerByConnection.Remove(connectionId, out playerId)) return false;

            _connectionByPlayer.Remove(playerId);
            return true;
        }

        #endregion

        /// <summary>
        /// 把连接上的消息转成玩家号再交给玩法回调。
        /// </summary>
        private sealed class PlayerHandler<T> where T : class, IMessage, new()
        {
            private readonly RoomManager _room;
            private readonly Action<uint, T> _callback;

            public PlayerHandler(RoomManager room, Action<uint, T> callback)
            {
                _room = room;
                _callback = callback;
            }

            public void Invoke(uint connectionId, T message)
            {
                if (!_room.TryGetPlayerId(connectionId, out uint playerId)) return;

                _callback(playerId, message);
            }
        }
    }
}
