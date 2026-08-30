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
            
            _server.RegisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, _battle.HandleGameJoinRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;
            
            _server.UnregisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, _battle.HandleGameJoinRequest);
            _server = null;
        }

        #endregion

        #region 发送消息

        public void SendGameJoinResponse(uint connectionId, bool accepted)
        {
            Game_Join_Response response = new() { Accepted = accepted, };
            _server.SendReliable(connectionId, NetEvent.GAME_JOIN_RESPONSE, response);
        }
        
        #endregion
    }
}
