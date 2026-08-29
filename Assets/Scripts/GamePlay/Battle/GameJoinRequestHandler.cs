using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 加入请求：只与战局名册和发送门面交互，不读取传输连接表。
    /// </summary>
    public sealed class GameJoinRequestHandler : IServerNetHandler<Game_Join_Request>
    {
        private readonly BattleSession _battle;

        public GameJoinRequestHandler(BattleSession battle)
        {
            _battle = battle;
        }

        public void Handle(uint senderId, Game_Join_Request message)
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
    }
}
