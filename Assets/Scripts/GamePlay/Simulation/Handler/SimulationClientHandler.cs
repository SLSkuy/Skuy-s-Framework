using System.Collections.Generic;
using Events;
using Framework;
using NetSync;
using Network;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端模拟收发：上传玩家输入，接收世界快照。只组包和解包。
    /// </summary>
    public sealed class SimulationClientHandler
    {
        private const int RecentInputCount = 8;

        private readonly ClientSimulationKernel _kernel;
        private readonly List<Input_Sample> _recentInputs = new();
        private NetClient _client;

        public SimulationClientHandler(ClientSimulationKernel kernel)
        {
            _kernel = kernel;
        }

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
        }

        #endregion

        #region 发送消息

        public void SendPlayerInput(uint inputTick, in InputState input)
        {
            Input_Sample sample = ProtoUtils.ToInputSample(inputTick, input);
            _recentInputs.Add(sample);
            if (_recentInputs.Count > RecentInputCount)
            {
                _recentInputs.RemoveAt(0);
            }

            Player_Input message = input.ToProto(inputTick);
            for (int i = 0; i < _recentInputs.Count; i++)
            {
                message.RecentInputs.Add(_recentInputs[i]);
            }

            _client.Send(NetEvent.GAME_PLAYER_INPUT, message);
        }

        #endregion

        #region 接收消息

        private void HandleWorldSnapshot(World_Snapshot message)
        {
            _kernel.HandleWorldSnapshot(message);
        }

        #endregion
    }
}
