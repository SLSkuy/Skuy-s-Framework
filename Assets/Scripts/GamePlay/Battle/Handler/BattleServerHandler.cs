using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局服务端模块：加入请求收发封装，自行登记与拆除回调。
    /// </summary>
    public sealed class BattleServerHandler
    {
        private readonly BattleManager _battle;
        private NetServer _server;

        public BattleServerHandler(BattleManager battle)
        {
            _battle = battle;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _server = Global.Get<NetServer>();
            _server.RegisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleGameJoinRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;
            
            _server.UnregisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleGameJoinRequest);
            _server = null;
        }

        #endregion

        #region 发送消息
        
        public void SendGameJoinResponse(uint connectionId, Game_Join_Response response)
        {
            _server.SendReliable(connectionId, NetEvent.GAME_JOIN_RESPONSE, response);
        }

        public void BroadcastRoster(Game_Join_Response response)
        {
            if (_server == null) return;

            _server.BroadcastReliable(NetEvent.GAME_JOIN_RESPONSE, response);
        }
        
        #endregion

        #region 接收消息

        private void HandleGameJoinRequest(uint connectionId, Game_Join_Request request)
        {
            Game_Join_Response response = _battle.HandleGameJoinRequest(connectionId, request);
            SendGameJoinResponse(connectionId, response);
            if (response.Accepted)
            {
                _server.BroadcastReliable(NetEvent.GAME_JOIN_RESPONSE, response);
            }
        }

        #endregion
    }
}
