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
            TryOpenRoom(true);
            
            _netServer = Global.Get<NetServer>();
            _netServer.OnClientRemoved += HandleClientRemoved;
            
            _serverHandler.Bind();
            _netServer.StartServer();
        }
        
        private void StopHost()
        {
            if (_netServer == null) return;

            _serverHandler.Unbind();
            _netServer.OnClientRemoved -= HandleClientRemoved;
            _netServer.StopServer();
            _netServer = null;
        }

        /// <summary>
        /// 作为客户端连接远端并在握手完成后发送加入请求。
        /// </summary>
        public void JoinRemoteRoom()
        {
            _activeRoom = new BattleRoom(BattleSessionRole.Client, false);
            _netClient = Global.Register<NetClient>();

            _clientHandler.Bind();
            _netClient.StartReliableConnect();
        }

        private void StopClient()
        {
            if (_netClient == null) return;

            _clientHandler.Unbind();
            _netClient.StopClient();
            _netClient = null;
        }
        
        private bool TryOpenRoom(bool acceptsRemoteJoin)
        {
            if (_activeRoom != null) return false;

            BattleRoom room = new(BattleSessionRole.Host, acceptsRemoteJoin);
            if (!room.TryAdmit(LOCAL_CONNECTION_ID))
            {
                return false;
            }

            _activeRoom = room;
            return true;
        }

        #endregion

        /// <summary>
        /// 开启战局
        /// </summary>
        public bool StartBattle()
        {
            return _activeRoom.StartMatch();
        }

        /// <summary>
        /// 结束对局，房间名册保留。
        /// </summary>
        public void StopBattle()
        {
            _activeRoom.EndMatch();
        }

        /// <summary>
        /// 将连接加入活动房间
        /// </summary>
        public bool Admit(uint connectionId)
        {
            if (_activeRoom == null) return false;
            if (connectionId != LOCAL_CONNECTION_ID && !_activeRoom.AcceptsRemoteJoin) return false;

            uint playerId = _nextPlayerId++;
            _playerId[connectionId] = playerId;
            
            return _activeRoom.TryAdmit(playerId);
        }

        /// <summary>
        /// 连接离开。房主离开则解散房间。
        /// </summary>
        public void Leave(uint connectionId)
        {
            if (_activeRoom == null) return;

            uint playerId = _playerId[connectionId];
            bool wasHost = playerId == _activeRoom.HostPlayerId;
            
            if (wasHost)
            {
                Dissolve();
            }
            else
            {
                _activeRoom.Leave(playerId);
            }
        }

        /// <summary>
        /// 解散活动房间并停止本进程为此房启动的网络。
        /// </summary>
        public void Dissolve()
        {
            if (_activeRoom != null)
            {
                _activeRoom.EndMatch();
                _activeRoom.ClearMembers();
                _activeRoom = null;
            }

            // 关闭当前连接
            StopHost();
            StopClient();
        }

        #region 子系统生命周期

        public override void Init()
        {
            _serverHandler = new BattleServerHandler(this);
            _clientHandler = new BattleClientHandler(this);
        }

        public override void Destroy()
        {
            Dissolve();
        }

        #endregion

        #region 网络消息处理

        public void HandleGameJoinRequest(uint connectionId, Game_Join_Request request)
        {
            _serverHandler.SendGameJoinResponse(connectionId, Admit(connectionId));
        }

        /// <summary>
        /// 客户端断连处理
        /// </summary>
        private void HandleClientRemoved(uint connectionId)
        {
            Leave(connectionId);
        }

        #endregion
    }
}
