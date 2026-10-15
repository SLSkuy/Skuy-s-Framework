using System.Collections.Generic;
using System.Linq;
using Events;
using Framework;
using GamePlay.GameSave;
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
        
        public void SendPlayerJoinResponse(uint connectionId, bool accepted, PlayerData player)
        {
            Room_Join_Response response = new()
            {
                Accepted = accepted,
            };

            if (accepted)
            {
                response.RoomCfg = _room.Config.ToProto();
                List<PlayerDataProto> infos = _room.GetPlayerInfos().Select(info => info.ToProto()).ToList();
                response.Remotes.AddRange(infos);
                
                _server.BroadcastReliable(NetEvent.ROOM_JOIN_RESPONSE, new Room_Player_Joined_Notify { Player = player.ToProto()});
            }
            
            _server.SendReliable(connectionId, NetEvent.ROOM_JOIN_RESPONSE, response);
        }
        
        public void BroadcastPlayerLeaved(string playerUUID)
        {
            _server.BroadcastReliable(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, new Room_Player_Leave_Notify { UUID = playerUUID });
        }

        /// <summary>
        /// 主机销毁通知
        /// </summary>
        public void BroadcastHostDissolved()
        {
            _server.BroadcastReliable(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, new Room_Server_Dissolved_Notify());
        }
        
        #endregion

        #region 接收消息

        /// <summary>
        /// 玩家加入请求
        /// </summary>
        private void HandleGameJoinRequest(uint connectionId, Room_Join_Request request)
        {
            _room.HandlePlayerJoinRequest(connectionId, request);
        }

        /// <summary>
        /// 玩家主动离开房间请求
        /// </summary>
        private void HandleGameLeaveRequest(uint connectionId, Room_Leave_Request request)
        {
            _room.HandlePlayerLeaveRequest(connectionId, request);
        }

        /// <summary>
        /// 玩家异常断开连接
        /// </summary>
        private void HandleClientRemoved(uint connectionId)
        {
            _room.HandlePlayerUnexpectedLeave(connectionId);
        }

        #endregion
    }
}
