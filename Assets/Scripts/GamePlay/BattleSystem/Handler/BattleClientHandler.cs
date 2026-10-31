using System;
using System.Collections.Generic;
using Events;
using Framework;
using NetSync;
using Network;
using Utils;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 战斗客户端网络消息处理器
    /// </summary>
    public class BattleClientHandler : IHandler
    {
        private const int RECENT_INPUT_COUNT = 4;
        
        private readonly List<Input_Command> _recentInputs = new();
        private NetClient _client;

        #region 事件
        public event Action<World_Snapshot> WorldSnapshotReceived;
        #endregion

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
        }

        public void Unbind()
        {
            _recentInputs.Clear();
            if (_client == null) return;

            _client.UnregisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client = null;
            
            WorldSnapshotReceived = null;
        }

        #endregion
        
        #region 发送消息

        public void SendPlayerInput(uint inputTick, in InputCommand input)
        {
            Input_Command command = input.ToProto(inputTick);
            _recentInputs.Add(command);
            if (_recentInputs.Count > RECENT_INPUT_COUNT)
            {
                _recentInputs.RemoveAt(0);
            }

            Player_Inputs message = new Player_Inputs { CurrentInputs = command };
            foreach (Input_Command inputSample in _recentInputs)
            {
                message.RecentInputs.Add(inputSample);
            }

            _client.Send(NetEvent.GAME_PLAYER_INPUT, message);
        }

        #endregion

        #region 接收消息
        
        private void HandleWorldSnapshot(World_Snapshot message)
        {
            WorldSnapshotReceived?.Invoke(message);
        }

        #endregion
    }
}