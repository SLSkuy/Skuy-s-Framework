using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Room
{
    /// <summary>
    /// 战局客户端模块
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
            _client.RegisterHandler<Game_Player_Joined>(NetEvent.GAME_PLAYER_JOINED, HandlePlayerJoined);
            _client.RegisterHandler<Game_Player_Leave_Notify>(NetEvent.GAME_LEAVE_NOTIFY, HandleGameLeaveNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;

            _client.UnregisterHandler<Game_Player_Joined>(NetEvent.GAME_PLAYER_JOINED, HandlePlayerJoined);
            _client.UnregisterHandler<Game_Player_Leave_Notify>(NetEvent.GAME_LEAVE_NOTIFY, HandleGameLeaveNotify);
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendGameLeaveRequest()
        {
            _client.SendReliable(NetEvent.GAME_LEAVE_REQUEST, new Game_Leave_Request());
        }

        #endregion

        #region 接受消息

        private void HandlePlayerJoined(Game_Player_Joined message)
        {
            // TODO: 处理新加入的玩家消息            
        }

        private void HandleGameLeaveNotify(Game_Player_Leave_Notify message)
        {
            _room.HandleGameLeaveNotify(message);
        }

        #endregion
    }
}
