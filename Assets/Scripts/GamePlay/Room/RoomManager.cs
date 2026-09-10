using System;
using System.Collections.Generic;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 对局连接与名册：入座、开听、连接映射与解散。不加载场景，不编排玩法。
    /// </summary>
    public sealed class RoomManager : SubSystemBase
    {
        private const int DEFAULT_CAPACITY = 4;
        private const int LOCAL_CAPACITY = 1;
        private const uint LOCAL_CONNECTION_ID = 0;

        private readonly Dictionary<uint, Player> _playersById = new();
        private Dictionary<uint, uint> _playerId; // connectionID -> playerID
        private uint _nextPlayerId;

        private RoomServerHandler _serverHandler;
        private RoomClientHandler _clientHandler;
        private NetServer _netServer;
        private NetClient _netClient;

        private bool _isInMatch;
        private SessionRole _sessionRole;
        private bool _acceptsRemoteJoin;
        private int _capacity;
        private uint _hostPlayerId;

        #region 属性
        public override int Priority => 150;
        public bool IsInMatch => _isInMatch;
        public SessionRole SessionRole => _sessionRole;
        public bool AcceptsRemoteJoin => _acceptsRemoteJoin;
        public int Capacity => _capacity;
        public int MemberCount => _playersById.Count;
        public uint HostPlayerId => _hostPlayerId;
        public bool IsListening => _netServer != null;
        #endregion

        #region 事件
        public event Action SessionEnded;
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
            if (_isInMatch)
            {
                return false;
            }

            _acceptsRemoteJoin = acceptsRemoteJoin;
            _capacity = acceptsRemoteJoin ? DEFAULT_CAPACITY : LOCAL_CAPACITY;
            _sessionRole = sessionRole;

            uint playerId = _nextPlayerId;
            if (!TryAdmitPlayer(playerId))
            {
                return false;
            }

            _nextPlayerId++;
            _playerId[LOCAL_CONNECTION_ID] = playerId;
            _isInMatch = true;
            return true;
        }

        private void StartListening()
        {
            if (!Global.TryGet(out _netServer))
            {
                _netServer = Global.Register<NetServer>();
            }

            _netServer.OnClientRemoved += HandleClientRemoved;
            _serverHandler.Bind();
            _netServer.StartServer();
        }

        private void StopHost()
        {
            if (_netServer == null)
            {
                return;
            }

            _netServer.OnClientRemoved -= HandleClientRemoved;
            _serverHandler.Unbind();
            _netServer.StopServer();
            Global.Unregister<NetServer>();

            _netServer = null;
        }

        /// <summary>
        /// 菜单加入被接受后，接管已有客户端连接并写入名册。
        /// </summary>
        public void CompleteClientJoin(Game_Join_Response response)
        {
            _netClient = Global.Get<NetClient>();
            _sessionRole = SessionRole.Client;
            _acceptsRemoteJoin = false;
            _capacity = DEFAULT_CAPACITY;
            ApplyRoster(response.HostPlayerId, response.PlayerIds);
            _isInMatch = true;
            _clientHandler.Bind();
        }

        private void StopClient()
        {
            if (_netClient == null)
            {
                return;
            }

            if (_isInMatch)
            {
                _clientHandler.SendGameLeaveRequest();
            }

            _clientHandler.Unbind();
            _netClient.StopClient();
            Global.Unregister<NetClient>();

            _netClient = null;
        }

        public uint[] GetPlayerIds()
        {
            uint[] playerIds = new uint[_playersById.Count];
            _playersById.Keys.CopyTo(playerIds, 0);
            return playerIds;
        }

        /// <summary>
        /// 应用远端发来的名单快照。
        /// </summary>
        public void ApplyRoster(uint hostPlayerId, IEnumerable<uint> playerIds)
        {
            ClearMembers();
            _hostPlayerId = hostPlayerId;
            foreach (uint playerId in playerIds)
            {
                TryAdmitPlayer(playerId);
            }
        }

        /// <summary>
        /// 将连接加入当前对局。
        /// </summary>
        public bool Admit(uint connectionId)
        {
            if (!_isInMatch) return false;
            if (connectionId != LOCAL_CONNECTION_ID && !_acceptsRemoteJoin) return false;
            if (_playerId.ContainsKey(connectionId)) return false;
            if (_playersById.Count >= _capacity) return false;
            
            uint playerId = _nextPlayerId;
            if (!TryAdmitPlayer(playerId)) return false;

            _nextPlayerId++;
            _playerId[connectionId] = playerId;
            
            return true;
        }

        /// <summary>
        /// 连接离开。房主离开则解散对局。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (!_isInMatch) return;
            if (!_playerId.Remove(connectionId, out uint playerId)) return;

            bool wasHost = playerId == _hostPlayerId;
            if (wasHost)
            {
                Dissolve();
                return;
            }

            RemovePlayer(playerId);
        }

        /// <summary>
        /// 处理房间关闭事件并通知其他玩家房间关闭
        /// </summary>
        private void Dissolve()
        {
            if (_isInMatch)
            {
                ClearMembers();
                _isInMatch = false;
            }

            if (_netServer != null)
            {
                _serverHandler.BroadcastLeave(new Game_Leave_Notify { Dissolved = true });
            }

            StopHost();
            StopClient();
        }

        private bool TryAdmitPlayer(uint playerId)
        {
            if (_playersById.Count >= _capacity) return false;

            Player player = new(playerId);
            _playersById[playerId] = player;

            if (_hostPlayerId == 0)
            {
                _hostPlayerId = playerId;
            }

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
            _hostPlayerId = 0;
            _playerId.Clear();
            _nextPlayerId = 1;
        }

        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            _nextPlayerId = 1;
            _playerId = new Dictionary<uint, uint>();
            _serverHandler = new RoomServerHandler(this);
            _clientHandler = new RoomClientHandler(this);
        }

        public override void Destroy()
        {
            Dissolve();
        }

        #endregion

        #region 网络消息处理

        private void HandleClientRemoved(uint connectionId)
        {
            Game_Leave_Notify notify = HandleGameLeaveRequest(connectionId);
            if (!notify.Dissolved && notify.PlayerId != 0)
            {
                _serverHandler.BroadcastLeave(notify);
            }
        }

        public Game_Leave_Notify HandleGameLeaveRequest(uint connectionId)
        {
            Game_Leave_Notify notify = new();
            if (!_isInMatch || !_playerId.TryGetValue(connectionId, out uint playerId))
            {
                notify.Dissolved = !_isInMatch;
                return notify;
            }

            notify.PlayerId = playerId;
            Leave(connectionId);
            notify.Dissolved = !_isInMatch;
            if (!_isInMatch)
            {
                return notify;
            }

            notify.HostPlayerId = _hostPlayerId;
            notify.PlayerIds.AddRange(GetPlayerIds());
            return notify;
        }

        public Game_Join_Response HandleGameJoinRequest(uint connectionId, Game_Join_Request request)
        {
            bool accepted = Admit(connectionId);

            Game_Join_Response response = new() { Accepted = accepted };
            if (!accepted || !_isInMatch)
            {
                return response;
            }

            if (_playerId.TryGetValue(connectionId, out uint playerId))
            {
                response.PlayerId = playerId;
            }

            response.HostPlayerId = _hostPlayerId;
            response.PlayerIds.AddRange(GetPlayerIds());

            return response;
        }

        public void HandleGameJoinResponse(Game_Join_Response response)
        {
            if (!response.Accepted)
            {
                HandleJoinFailed();
                return;
            }

            if (!_isInMatch)
            {
                return;
            }

            ApplyRoster(response.HostPlayerId, response.PlayerIds);
        }

        public void HandleJoinFailed()
        {
            if (_isInMatch)
            {
                return;
            }

            StopClient();
        }

        public void HandleGameLeaveNotify(Game_Leave_Notify notify)
        {
            if (notify.Dissolved)
            {
                bool hadSession = _isInMatch || _netClient != null;
                Dissolve();
                if (hadSession)
                {
                    SessionEnded?.Invoke();
                }

                return;
            }

            if (!_isInMatch)
            {
                return;
            }

            ApplyRoster(notify.HostPlayerId, notify.PlayerIds);
        }

        #endregion
    }
}
