using Events;
using Framework;
using GamePlay.GameData;
using NetConnect;
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
            _client.RegisterHandler<Room_Join_Response>(NetEvent.ROOM_JOIN_RESPONSE, HandleJoinResponse);
            _client.RegisterHandler<Room_Player_Joined_Notify>(NetEvent.ROOM_PLAYER_JOINED_NOTIFY, HandlePlayerJoinNotify);
            _client.RegisterHandler<Room_Player_Leave_Notify>(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, HandlePlayerLeaveNotify);
            _client.RegisterHandler<Room_Server_Dissolved_Notify>(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, HandleServerDissolveNotify);
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.UnregisterHandler<Room_Join_Response>(NetEvent.ROOM_JOIN_RESPONSE, HandleJoinResponse);
            _client.UnregisterHandler<Room_Player_Joined_Notify>(NetEvent.ROOM_PLAYER_JOINED_NOTIFY, HandlePlayerJoinNotify);
            _client.UnregisterHandler<Room_Player_Leave_Notify>(NetEvent.ROOM_PLAYER_LEAVE_NOTIFY, HandlePlayerLeaveNotify);
            _client.UnregisterHandler<Room_Server_Dissolved_Notify>(NetEvent.ROOM_HOST_DISSOLVED_NOTIFY, HandleServerDissolveNotify);
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendJoinRequest(PlayerInfoData playerInfo)
        {
            _client.SendReliable(NetEvent.ROOM_JOIN_REQUEST, new Room_Join_Request { Player = playerInfo.ToProto() });
        }

        public void SendGameLeaveRequest()
        {
            _client.SendReliable(NetEvent.ROOM_LEAVE_REQUEST, new Room_Leave_Request());
        }

        #endregion

        #region 接收消息

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response response)
        {
            // TODO: 由独立模块处理连接，现临时使用
            _room.HandleReliableConnectResponse(response);
        }

        private void HandleJoinResponse(Room_Join_Response response)
        {
            _room.HandleJoinResponse(response);
        }

        private void HandlePlayerJoinNotify(Room_Player_Joined_Notify message)
        {
            _room.HandlePlayerJoinNotify(message);
        }

        private void HandlePlayerLeaveNotify(Room_Player_Leave_Notify message)
        {
            _room.HandlePlayerLeaveNotify(message);
        }

        private void HandleServerDissolveNotify(Room_Server_Dissolved_Notify message)
        {
            _room.HandleServerDissolveNotify(message);
        }

        #endregion
    }
}
