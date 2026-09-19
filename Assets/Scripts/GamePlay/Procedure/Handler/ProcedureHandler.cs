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
            _client.ConnectionFailed += HandleConnectionFailed;
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.RegisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.ConnectionFailed -= HandleConnectionFailed;
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.UnregisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
            _client = null;
        }

        #endregion

        #region 发送消息

        private void SendGameJoinRequest()
        {
            Game_Join_Request request = new();
            _client.SendReliable(NetEvent.GAME_JOIN_REQUEST, request);
        }

        #endregion

        #region 接收消息

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response message)
        {
            SendGameJoinRequest();
        }

        private void HandleGameJoinResponse(Game_Join_Response message)
        {
            Unbind();
            _procedure.HandleGameJoinResponse(message);
        }

        private void HandleConnectionFailed()
        {
            _procedure.HandleConnectionFailed();
        }

        #endregion
    }
}
