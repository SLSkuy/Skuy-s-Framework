using System.Collections.Generic;
using System.Linq;
using Events;
using Framework;
using GamePlay.GameData;
using NetSync;
using Network;
using Utils;

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
        
        public void SendPlayerJoinResponse(uint connectionId, bool accepted, PlayerInfoData playerInfo)
        {
            Room_Join_Response response = new() { Accepted = accepted };

            if (accepted)
            {
                response.RoomCfg = _room.Config.ToProto();
                List<PlayerInfoDataProto> infos = _room.GetAllPlayerData().Select(info => info.ToProto()).ToList();
                response.Remotes.AddRange(infos);

                uint[] activeIDs = _room.GetAllPlayerConnectionID();
                Room_Player_Joined_Notify notify = new Room_Player_Joined_Notify { Player = playerInfo.ToProto() };
                _server.SendGroupReliableWithout(connectionId, activeIDs, NetEvent.ROOM_PLAYER_JOINED_NOTIFY, notify);
            }
            
            _server.SendReliable(connectionId, NetEvent.ROOM_JOIN_RESPONSE, response);
        }
        
        public void BroadcastPlayerLeaved(string playerUUID)
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            Room_Player_Leave_Notify notify = new Room_Player_Leave_Notify { UUID = playerUUID };
            _server.SendGroupReliable(activeIDs,  NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, notify);
        }

        /// <summary>
        /// 主机销毁通知
        /// </summary>
        public void BroadcastHostDissolved()
        {
            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            Room_Server_Dissolved_Notify notify = new Room_Server_Dissolved_Notify();
            _server.SendGroupReliable(activeIDs,  NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, notify);
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
