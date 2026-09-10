using System;
using Events;
using Framework;
using GamePlay.Room;
using NetConnect;
using NetSync;
using Network;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 菜单加入阶段：只登记客户端网络，被接受前不创建名册对象。
    /// </summary>
    public sealed class ProcedureJoinClientHandler
    {
        public event Action<bool> JoinSettled;

        private NetClient _client;

        public void StartJoin()
        {
            _client = Global.Register<NetClient>();
            Bind();
            _client.StartReliableConnect();
        }

        public void CancelJoin()
        {
            Unbind();
            if (_client == null)
            {
                return;
            }

            _client.StopClient();
            Global.Unregister<NetClient>();
            _client = null;
        }

        private void Bind()
        {
            Unbind();
            _client.ConnectionFailed += HandleConnectionFailed;
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.RegisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
        }

        private void Unbind()
        {
            if (_client == null)
            {
                return;
            }

            _client.ConnectionFailed -= HandleConnectionFailed;
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.UnregisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
        }

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response message)
        {
            Game_Join_Request request = new() { ClientId = _client.ClientId };
            _client.SendReliable(NetEvent.GAME_JOIN_REQUEST, request);
        }

        private void HandleGameJoinResponse(Game_Join_Response message)
        {
            Unbind();
            if (!message.Accepted)
            {
                CancelJoin();
                JoinSettled?.Invoke(false);
                return;
            }

            RoomManager room = Global.Register<RoomManager>();
            room.CompleteClientJoin(message);
            _client = null;
            JoinSettled?.Invoke(true);
        }

        private void HandleConnectionFailed()
        {
            CancelJoin();
            JoinSettled?.Invoke(false);
        }
    }
}
