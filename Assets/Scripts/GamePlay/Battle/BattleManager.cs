using System;
using System.Collections.Generic;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局管理器：本进程建房、入房、开战入口，以及网络启停。名册与对局对象在房间内。
    /// </summary>
    public sealed class BattleManager : SubSystemBase
    {
        private const uint LOCAL_CONNECTION_ID = 0;

        private Dictionary<uint, uint> _playerId; // connectionID -> playerID
        private uint _nextPlayerId;
        
        private BattleServerHandler _serverHandler;
        private BattleClientHandler _clientHandler;
        private BattleRoom _activeRoom;
        private NetServer _netServer;
        private NetClient _netClient;

        #region 属性
        public override int Priority => 150;
        public BattleRoom ActiveRoom => _activeRoom;
        public bool IsMatchSubmitted => _activeRoom is { IsMatchSubmitted: true };
        public bool IsListening => _netServer is { IsRunning: true };
        #endregion
        
        #region 事件
        public event Action<bool> JoinSettled;
        public event Action SessionEnded;
        #endregion

        #region 房间管理

        /// <summary>
        /// 创建不接远端的房间，并让本机玩家入座。
        /// </summary>
        public void CreateLocalRoom()
        {
            TryOpenRoom(false);
        }

        /// <summary>
        /// 创建接远端的房间，本机入座后开启监听。
        /// </summary>
        public void CreateHostRoom()
        {
            if (!TryOpenRoom(true))
            {
                return;
            }

            if (!Global.TryGet(out _netServer))
            {
                _netServer = Global.Register<NetServer>();
            }

            _netServer.OnClientRemoved += HandleClientRemoved;
            _serverHandler.Bind();
            _netServer.StartServer();
        }
        
        /// <summary>
        /// 主机关闭
        /// </summary>
        private void StopHost()
        {
            if (_netServer == null) return;
            
            _netServer.OnClientRemoved -= HandleClientRemoved;
            _serverHandler.Unbind();
            _netServer.StopServer();
            Global.Unregister<NetServer>();
            
            _netServer = null;
        }

        /// <summary>
        /// 作为客户端连接远端并在握手完成后发送加入请求。
        /// </summary>
        public void JoinRemoteRoom()
        {
            if (!Global.TryGet(out _netClient))
            {
                _netClient = Global.Register<NetClient>();
            }

            _clientHandler.Bind();
            _netClient.StartReliableConnect();
        }

        /// <summary>
        /// 客户端关闭连接
        /// </summary>
        private void StopClient()
        {
            if (_netClient == null) return;

            _clientHandler.Unbind();
            _netClient.StopClient();
            Global.Unregister<NetClient>();
            
            _netClient = null;
        }
        
        /// <summary>
        /// 尝试开启战局房间
        /// </summary>
        /// <param name="acceptsRemoteJoin"></param>
        /// <returns></returns>
        private bool TryOpenRoom(bool acceptsRemoteJoin)
        {
            if (_activeRoom != null) return false;

            int capacity = acceptsRemoteJoin ? BattleRoom.DEFAULT_CAPACITY : BattleRoom.LOCAL_CAPACITY;
            BattleRoom room = new(BattleSessionRole.Host, acceptsRemoteJoin, capacity);
            
            uint playerId = _nextPlayerId;
            if (!room.TryAdmit(playerId)) return false;

            _nextPlayerId++;
            _playerId[LOCAL_CONNECTION_ID] = playerId;
            room.SubmitMatch();
            _activeRoom = room;
            
            return true;
        }

        #endregion

        /// <summary>
        /// 开启战局
        /// </summary>
        public bool StartBattle()
        {
            if (_activeRoom == null)
            {
                return false;
            }

            return _activeRoom.StartMatch();
        }

        /// <summary>
        /// 结束对局，房间名册保留。
        /// </summary>
        public void StopBattle()
        {
            _activeRoom?.EndMatch();
        }

        /// <summary>
        /// 将连接加入活动房间
        /// </summary>
        public bool Admit(uint connectionId)
        {
            if (_activeRoom == null) return false;
            if (connectionId != LOCAL_CONNECTION_ID && !_activeRoom.AcceptsRemoteJoin) return false;
            if (_playerId.ContainsKey(connectionId)) return false;
            if (_activeRoom.MemberCount >= _activeRoom.Capacity) return false;

            uint playerId = _nextPlayerId;
            if (!_activeRoom.TryAdmit(playerId)) return false;

            _nextPlayerId++;
            _playerId[connectionId] = playerId;
            
            return true;
        }

        /// <summary>
        /// 连接离开。房主离开则解散房间。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (_activeRoom == null) return;
            if (!_playerId.Remove(connectionId, out uint playerId)) return;

            bool wasHost = playerId == _activeRoom.HostPlayerId;
            if (wasHost)
            {
                Dissolve();
                return;
            }

            _activeRoom.Leave(playerId);
        }

        /// <summary>
        /// 解散活动房间并停止本进程为此房启动的网络。
        /// </summary>
        private void Dissolve()
        {
            if (_activeRoom != null)
            {
                _activeRoom.EndMatch();
                _activeRoom.ClearMembers();
                _activeRoom = null;
            }

            if (_netServer != null)
            {
                _serverHandler.BroadcastRoster(new Game_Join_Response { Accepted = false });
            }

            StopHost();
            StopClient();
        }

        #region 子系统生命周期

        public override void Init()
        {
            _nextPlayerId = 1;
            _playerId = new Dictionary<uint, uint>();
            _serverHandler = new BattleServerHandler(this);
            _clientHandler = new BattleClientHandler(this);
        }

        public override void Destroy()
        {
            Dissolve();
        }

        #endregion

        #region 网络消息处理
        
        /// <summary>
        /// 客户端断连处理
        /// </summary>
        private void HandleClientRemoved(uint connectionId)
        {
            Leave(connectionId);

            Game_Join_Response response = new() { Accepted = _activeRoom != null };
            if (_activeRoom != null)
            {
                response.HostPlayerId = _activeRoom.HostPlayerId;
                response.InMatch = _activeRoom.IsMatchSubmitted;
                List<uint> roster = new();
                _activeRoom.CopyPlayerIds(roster);
                response.PlayerIds.AddRange(roster);
            }

            if (response.Accepted)
            {
                _serverHandler.BroadcastRoster(response);
            }
        }

        public Game_Join_Response HandleGameJoinRequest(uint connectionId, Game_Join_Request request)
        {
            bool accepted = Admit(connectionId);
            
            Game_Join_Response response = new() { Accepted = accepted };
            if (!accepted || _activeRoom == null)
            {
                return response;
            }

            if (_playerId.TryGetValue(connectionId, out uint playerId))
            {
                response.PlayerId = playerId;
            }

            response.HostPlayerId = _activeRoom.HostPlayerId;
            response.InMatch = _activeRoom.IsMatchSubmitted;
            List<uint> roster = new();
            _activeRoom.CopyPlayerIds(roster);
            response.PlayerIds.AddRange(roster);

            return response;
        }

        public void HandleGameJoinResponse(Game_Join_Response response)
        {
            if (!response.Accepted)
            {
                if (_activeRoom != null)
                {
                    // 关闭已经创建的房间
                    bool hadSession = _activeRoom != null || _netClient != null;
                    Dissolve();
                    if (hadSession)
                    {
                        SessionEnded?.Invoke();
                    }
                    return;
                }

                HandleJoinFailed();
                return;
            }

            if (_activeRoom != null)
            {
                _activeRoom.ApplyRoster(response.HostPlayerId, response.PlayerIds, response.InMatch);
                return;
            }

            BattleRoom room = new(BattleSessionRole.Client, false);
            room.ApplyRoster(response.HostPlayerId, response.PlayerIds, response.InMatch);
            _activeRoom = room;
            JoinSettled?.Invoke(true);
        }

        public void HandleJoinFailed()
        {
            if (_activeRoom != null)
            {
                return;
            }

            StopClient();
            JoinSettled?.Invoke(false);
        }

        #endregion
    }
}
