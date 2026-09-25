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
            _server.RegisterHandler<Room_Join_Request>(NetEvent.ROOM_JOIN_REQUEST, HandleGameJoinRequest);
            _server.RegisterHandler<Room_Leave_Request>(NetEvent.ROOM_LEAVE_REQUEST, HandleGameLeaveRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;

            _server.OnClientRemoved -= HandleClientRemoved;
            _server.UnregisterHandler<Room_Join_Request>(NetEvent.ROOM_JOIN_REQUEST, HandleGameJoinRequest);
            _server.UnregisterHandler<Room_Leave_Request>(NetEvent.ROOM_LEAVE_REQUEST, HandleGameLeaveRequest);
            _server = null;
        }

        #endregion

        #region 发送消息
        
        public void SendGameJoinResponse(uint connectionId)
        {
            uint playerId = _room.Admit(connectionId);
            
            Room_Join_Response response = new()
            {
                Accepted = playerId != 0 && _room.IsInMatch,
                PlayerId = playerId,
                HostPlayerId = _room.HostPlayerId
            };
            response.PlayerIds.AddRange(_room.GetPlayerIds());
            
            // 返回加入结果，若加入成功则通知房间内的其他玩家有新玩家加入
            _server.SendReliable(connectionId, NetEvent.ROOM_JOIN_RESPONSE, response);
            if (response.Accepted)
            {
                BroadcastPlayerJoined(new Room_Player_Joined_Notify { PlayerId = playerId });
            }
        }

        public void BroadcastPlayerJoined(Room_Player_Joined_Notify notify)
        {
            _server.BroadcastReliable(NetEvent.ROOM_PLAYER_JOINED_NOTIFY, notify);
        }

        public void BroadcastPlayerLeaved(Room_Player_Leave_Notify notify)
        {
            _server.BroadcastReliable(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, notify);
        }

        /// <summary>
        /// 主机销毁通知
        /// </summary>
        public void BroadcastHostDissolved(Room_Host_Dissolved_Notify notify)
        {
            _server.BroadcastReliable(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, notify);
        }
        
        #endregion

        #region 接收消息

        /// <summary>
        /// 玩家加入请求
        /// </summary>
        private void HandleGameJoinRequest(uint connectionId, Room_Join_Request request)
        {
            SendGameJoinResponse(connectionId);
        }

        /// <summary>
        /// 玩家主动离开房间请求
        /// </summary>
        private void HandleGameLeaveRequest(uint connectionId, Room_Leave_Request request)
        {
            NotifyLeave(connectionId);
        }

        /// <summary>
        /// 玩家连接断开
        /// </summary>
        private void HandleClientRemoved(uint connectionId)
        {
            NotifyLeave(connectionId);
        }

        private void NotifyLeave(uint connectionId)
        {
            uint playerId = _room.HandlePlayerLeaveRequest(connectionId);
            if (playerId != 0 && _room.IsInMatch)
            {
                BroadcastPlayerLeaved(new Room_Player_Leave_Notify { PlayerId = playerId });
            }
        }

        #endregion
    }
}
