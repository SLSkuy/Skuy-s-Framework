using Events;
using Framework;
using NetConnect;
using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局客户端模块：加入请求发送与加入响应接收。
    /// </summary>
    public sealed class BattleClientHandler
    {
        private BattleManager _battle;
        private NetClient _client;

        public BattleClientHandler(BattleManager battle)
        {
            _battle = battle;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.RegisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
        }

        public void Unbind()
        {
            if (_client == null) return;
            _client.UnregisterHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            _client.UnregisterHandler<Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleGameJoinResponse);
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

        #endregion

        #region 接受消息

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response message)
        {
            SendGameJoinRequest();
        }

        private void HandleGameJoinResponse(Game_Join_Response message)
        {
            _battle.HandleGameJoinResponse(message);
        }

        #endregion
    }
}
