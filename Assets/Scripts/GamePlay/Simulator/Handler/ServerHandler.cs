using System;
using Events;
using Framework;
using NetSync;
using Network;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 服务端消息处理器
    /// </summary>
    public class ServerHandler
    {
        private NetServer _netServer;

        #region 事件
        public event Action<uint, Game_Join_Request> OnJoinRequest;
        public event Action<uint, Player_Input> OnPlayerInput;
        #endregion

        #region 消息绑定

        public bool BindMessage()
        {
            _netServer = Global.Get<NetServer>();
            if (_netServer == null) return false;

            _netServer.RegNetHandler(NetEvent.GAME_JOIN_REQUEST, OnJoinRequest);
            _netServer.RegNetHandler(NetEvent.PLAYER_INPUT, OnPlayerInput);
            return true;
        }

        public void UnBindMessage()
        {
            if (_netServer == null) return;
            _netServer.UnRegNetHandler(NetEvent.GAME_JOIN_REQUEST);
            _netServer.UnRegNetHandler(NetEvent.PLAYER_INPUT);
            _netServer = null;
        }

        #endregion

        #region 发送消息

        public void SendJoinResponse(uint clientId, bool accepted)
        {
            if (_netServer == null || !_netServer.IsRunning) return;

            Game_Join_Response joinResponse = new() { Accepted = accepted };
            _netServer.SendReliable(clientId, NetEvent.GAME_JOIN_RESPONSE, joinResponse);
        }

        public void BroadcastWorldSnapshot(World_Snapshot worldSnapshot)
        {
            if (_netServer == null || !_netServer.IsRunning) return;

            _netServer.Broadcast(NetEvent.WORLD_SNAPSHOT, worldSnapshot);
        }

        #endregion
    }
}
