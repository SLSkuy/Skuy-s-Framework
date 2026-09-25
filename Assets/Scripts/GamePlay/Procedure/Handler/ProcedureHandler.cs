using Events;
using Framework;
using NetConnect;
using NetSync;
using Network;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 菜单加入阶段：可靠连接与加入请求收发，被接受前不创建名册对象。
    /// </summary>
    public sealed class ProcedureHandler
    {
        private readonly ProcedureCore _procedure;
        private NetClient _client;

        public ProcedureHandler(ProcedureCore procedure)
        {
            _procedure = procedure;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.OnConnectionFailed += HandleOnConnectionFailed;
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.RegisterHandler<Room_Join_Response>(NetEvent.ROOM_JOIN_RESPONSE, HandleGameJoinResponse);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.OnConnectionFailed -= HandleOnConnectionFailed;
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.UnregisterHandler<Room_Join_Response>(NetEvent.ROOM_JOIN_RESPONSE, HandleGameJoinResponse);
            _client = null;
        }

        #endregion

        #region 发送消息

        private void SendGameJoinRequest()
        {
            Room_Join_Request request = new();
            _client.SendReliable(NetEvent.ROOM_JOIN_REQUEST, request);
        }

        #endregion

        #region 接收消息

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response message)
        {
            SendGameJoinRequest();
        }

        private void HandleGameJoinResponse(Room_Join_Response message)
        {
            _procedure.HandleGameJoinResponse(message);
        }

        private void HandleOnConnectionFailed()
        {
            _procedure.HandleConnectionFailed();
        }

        #endregion
    }
}
