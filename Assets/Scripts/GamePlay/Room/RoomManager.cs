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
            _playerId[LOCAL_CONNECTION_ID] = playerId;
            LocalPlayerId = playerId;
            IsInMatch = true;
            return true;
        }

        private void StartListening()
        {
            if (!Global.TryGet(out _netServer))
            {
                _netServer = Global.Register<NetServer>();
            }

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
            Global.Unregister<NetServer>();

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
        /// 将连接加入当前对局。
        /// </summary>
        public uint Admit(uint connectionId)
        {
            if (!IsInMatch) return 0;
            if (connectionId != LOCAL_CONNECTION_ID && !AcceptsRemoteJoin) return 0;
            if (_playerId.ContainsKey(connectionId)) return 0;
            if (_playersById.Count >= Capacity) return 0;
            
            uint playerId = _nextPlayerId;
            if (!TryAdmitPlayer(playerId)) return 0;

            _nextPlayerId++;
            _playerId[connectionId] = playerId;
            
            return playerId;
        }

        /// <summary>
        /// 连接离开。房主离开则解散对局。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (!IsInMatch) return;
            if (!_playerId.Remove(connectionId, out uint playerId)) return;

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
                _serverHandler.BroadcastPlayerLeaved(new Game_Player_Leave_Notify { Dissolved = true });
            }

            StopHost();
            StopClient();
        }

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
            _playerId.Clear();
            _nextPlayerId = 1;
            HostPlayerId = 0;
            LocalPlayerId = 0;
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
        
        /// <summary>
        /// 菜单加入被接受后，接管已有客户端连接并写入名册。
        /// </summary>
        public void HandleGameJoinResponseJoin(Game_Join_Response response)
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
        /// 按连接离座。未入座或对局已不存在时返回 0。
        /// </summary>
        public uint HandleGameLeaveRequest(uint connectionId)
        {
            if (!IsInMatch || !_playerId.TryGetValue(connectionId, out uint playerId))
            {
                return 0;
            }

            Leave(connectionId);
            return playerId;
        }

        /// <summary>
        /// 应用远端离开通知。解散则清名册并结束本机会话。
        /// </summary>
        public void HandleGameLeaveNotify(Game_Player_Leave_Notify notify)
        {
            if (notify.Dissolved)
            {
                Dissolve();
                SessionEnded?.Invoke();
                return;
            }

            if (!IsInMatch) return;

            RemovePlayer(notify.PlayerId);
            PlayerRemoved?.Invoke(notify.PlayerId);
        }

        #endregion
    }
}
