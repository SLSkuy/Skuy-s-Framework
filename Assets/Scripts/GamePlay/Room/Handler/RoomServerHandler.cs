using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 战局服务端：Join_Response 只回给申请连接；名册增量与世界 Spawn 分开。
    /// </summary>
    public sealed class RoomServerHandler
    {
        private readonly RoomManager _room;
        private NetServer _server;

        public RoomServerHandler(RoomManager room)
        {
            _room = room;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _server = Global.Get<NetServer>();
            _server.OnClientRemoved += HandleClientRemoved;
            _server.RegisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleGameJoinRequest);
            _server.RegisterHandler<Game_Leave_Request>(NetEvent.GAME_LEAVE_REQUEST, HandleGameLeaveRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;

            _server.OnClientRemoved -= HandleClientRemoved;
            _server.UnregisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleGameJoinRequest);
            _server.UnregisterHandler<Game_Leave_Request>(NetEvent.GAME_LEAVE_REQUEST, HandleGameLeaveRequest);
            _server = null;
        }

        #endregion

        #region 发送消息
        
        public void SendGameJoinResponse(uint connectionId)
        {
            uint playerId = _room.Admit(connectionId);
            
            Game_Join_Response response = new()
            {
                Accepted = playerId != 0 && _room.IsInMatch,
                HostPlayerId = _room.HostPlayerId
            };
            response.PlayerIds.AddRange(_room.GetPlayerIds());
            
            _server.SendReliable(connectionId, NetEvent.GAME_JOIN_RESPONSE, response);
        }

        public void BroadcastPlayerJoined(Game_Player_Joined notify)
        {
            _server.BroadcastReliable(NetEvent.GAME_PLAYER_JOINED, notify);
        }

        public void BroadcastPlayerLeaved(Game_Player_Leave_Notify notify)
        {
            _server.BroadcastReliable(NetEvent.GAME_LEAVE_NOTIFY, notify);
        }
        
        #endregion

        #region 接收消息

        /// <summary>
        /// 玩家加入请求
        /// </summary>
        private void HandleGameJoinRequest(uint connectionId, Game_Join_Request request)
        {
            SendGameJoinResponse(connectionId);
        }

        private void HandleGameLeaveRequest(uint connectionId, Game_Leave_Request request)
        {
            NotifyLeave(connectionId);
        }

        private void HandleClientRemoved(uint connectionId)
        {
            NotifyLeave(connectionId);
        }

        private void NotifyLeave(uint connectionId)
        {
            uint playerId = _room.HandleGameLeaveRequest(connectionId);
            if (playerId != 0 && _room.IsInMatch)
            {
                BroadcastPlayerLeaved(new Game_Player_Leave_Notify { PlayerId = playerId });
            }
        }

        #endregion
    }
}
