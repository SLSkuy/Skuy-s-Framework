using System;
using Events;
using Framework;
using NetSync;
using Network;
using Utils;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 客户端消息处理器
    /// </summary>
    public class ClientHandler
    {
        private NetClient _netClient;

        #region 属性
        public uint ClientId => _netClient?.ClientId ?? 0;
        #endregion

        #region 事件
        public event Action<Game_Join_Response> OnJoinResponse;
        public event Action<World_Snapshot> OnWorldSnapshot;
        #endregion

        #region 消息绑定

        public bool BindMessage()
        {
            _netClient = Global.Get<NetClient>();
            if (_netClient == null) return false;

            _netClient.RegNetHandler(NetEvent.GAME_JOIN_RESPONSE, OnJoinResponse);
            _netClient.RegNetHandler(NetEvent.WORLD_SNAPSHOT, OnWorldSnapshot);
            return true;
        }

        public void UnbindMessage()
        {
            if (_netClient == null) return;
            _netClient.UnRegNetHandler(NetEvent.GAME_JOIN_RESPONSE);
            _netClient.UnRegNetHandler(NetEvent.WORLD_SNAPSHOT);
            _netClient = null;
        }

        #endregion

        #region 发送消息

        public void SendJoinRequest()
        {
            if (_netClient == null || !_netClient.IsRunning || ClientId == 0) return;

            Game_Join_Request joinRequest = new() { ClientId = ClientId };
            _netClient.SendReliable(NetEvent.GAME_JOIN_REQUEST, joinRequest);
        }

        public void SendPlayerInput(uint inputTick, InputState inputState)
        {
            if (_netClient == null || !_netClient.IsRunning) return;

            Player_Input input = ProtoUtils.ToPlayerInput(ClientId, inputTick, inputState);
            _netClient.SendReliable(NetEvent.PLAYER_INPUT, input);
        }

        #endregion
    }
}
