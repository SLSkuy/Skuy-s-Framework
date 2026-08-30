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
        private BattleManager _battle;
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
            _server.RegisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, OnGameJoinRequest);
        }

        public void Unbind()
        {
            if (_server == null) return;
            _server.UnregisterHandler<Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, OnGameJoinRequest);
            _server = null;
        }

        #endregion

        #region 发送消息

        public void SendGameJoinResponse(uint connectionId, bool accepted, uint playerId, string sceneName)
        {
            Game_Join_Response response = new() { Accepted = accepted, };
            _server.SendReliable(connectionId, NetEvent.GAME_JOIN_RESPONSE, response);
        }

        public void BroadcastWorldSnapshot()
        {
        }

        #endregion

        #region 接受消息

        private void OnGameJoinRequest(uint senderId, Game_Join_Request message)
        {
            bool accepted = _battle.Admit(senderId, out uint playerId);
            string sceneName = accepted ? _battle.ActiveRoom.PlaySceneName : string.Empty;
            SendGameJoinResponse(senderId, accepted, playerId, sceneName);
        }

        #endregion
    }
}
