using Events;
using Framework;
using NetConnect;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 战局客户端模块：加入与离开请求发送，加入响应与离开通知接收。
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
            _client.ConnectionFailed += HandleConnectionFailed;
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.RegisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
            _client.RegisterHandler<Game_Leave_Notify>(NetEvent.GAME_LEAVE_NOTIFY, HandleGameLeaveNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.ConnectionFailed -= HandleConnectionFailed;
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.UnregisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
            _client.UnregisterHandler<Game_Leave_Notify>(NetEvent.GAME_LEAVE_NOTIFY, HandleGameLeaveNotify);
            _client = null;
        }

        #endregion

        #region 发送消息

        /// <summary>
        /// 填充并发送加入请求。
        /// </summary>
        public void SendGameJoinRequest()
        {
            Game_Join_Request request = new() { ClientId = _client.ClientId };
            _client.SendReliable(NetEvent.GAME_JOIN_REQUEST, request);
        }

        public void SendGameLeaveRequest()
        {
            _client.SendReliable(NetEvent.GAME_LEAVE_REQUEST, new Game_Leave_Request());
        }

        #endregion

        #region 接受消息

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response message)
        {
            SendGameJoinRequest();
        }

        private void HandleGameJoinResponse(Game_Join_Response message)
        {
            _room.HandleGameJoinResponse(message);
        }

        private void HandleGameLeaveNotify(Game_Leave_Notify message)
        {
            _room.HandleGameLeaveNotify(message);
        }

        private void HandleConnectionFailed()
        {
            _room.HandleJoinFailed();
        }

        #endregion
    }
}
