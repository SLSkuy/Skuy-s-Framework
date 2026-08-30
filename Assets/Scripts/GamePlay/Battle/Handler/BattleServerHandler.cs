using Events;
using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 战局服务端模块：加入请求收发封装，自行登记与拆除回调。
    /// </summary>
    public sealed class BattleServerHandler
    {
        private readonly NetServer _server;
        private readonly BattleSession _battle;

        public BattleServerHandler(NetServer server, BattleSession battle)
        {
            _server = server;
            _battle = battle;
        }

        #region 消息绑定
        
        public void Bind()
        {
            Unbind();
            _server.RegisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, OnGameJoinRequest);
        }
        
        public void Unbind()
        {
            _server.UnregisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, OnGameJoinRequest);
        }

        #endregion

        #region 发送消息

        public void BroadcastWorldSnapshot()
        {
            
        }

        #endregion

        #region 接受消息

        private void OnGameJoinRequest(uint senderId, Game_Join_Request message)
        {
            if (message == null)
            {
                _battle.TryJoin(senderId, out _);
                return;
            }

            uint clientId = message.ClientId != 0 ? message.ClientId : senderId;
            if (message.ClientId != 0 && message.ClientId != senderId)
            {
                _battle.RejectJoin(senderId);
                return;
            }

            _battle.TryJoin(clientId, out _);
        }

        #endregion
    }
}
