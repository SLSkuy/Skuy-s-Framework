using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 战局客户端模块
    /// </summary>
    public sealed class RoomClientHandler
    {
        private readonly RoomManager _room;
        private NetClient _client;

        public RoomClientHandler(RoomManager room)
        {
            _room = room;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<Room_Player_Joined_Notify>(NetEvent.ROOM_PLAYER_JOINED_NOTIFY, HandlePlayerJoined);
            _client.RegisterHandler<Room_Player_Leave_Notify>(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, HandleGameLeaveNotify);
            _client.RegisterHandler<Room_Host_Dissolved_Notify>(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, HandleHostDissolvedNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.UnregisterHandler<Room_Player_Joined_Notify>(NetEvent.ROOM_PLAYER_JOINED_NOTIFY, HandlePlayerJoined);
            _client.UnregisterHandler<Room_Player_Leave_Notify>(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, HandleGameLeaveNotify);
            _client.UnregisterHandler<Room_Host_Dissolved_Notify>(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, HandleHostDissolvedNotify);
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendGameLeaveRequest()
        {
            _client.SendReliable(NetEvent.ROOM_LEAVE_REQUEST, new Room_Leave_Request());
        }

        #endregion

        #region 接收消息

        private void HandlePlayerJoined(Room_Player_Joined_Notify message)
        {
            _room.HandlePlayerJoinedNotify(message);
        }

        private void HandleGameLeaveNotify(Room_Player_Leave_Notify message)
        {
            _room.HandlePlayerLeaveNotify(message);
        }

        private void HandleHostDissolvedNotify(Room_Host_Dissolved_Notify message)
        {
            _room.HandleHostDissolvedNotify(message);
        }

        #endregion
    }
}
