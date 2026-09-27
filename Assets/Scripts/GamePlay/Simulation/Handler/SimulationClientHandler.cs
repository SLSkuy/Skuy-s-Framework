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
        private readonly ClientSimulationKernel _kernel;
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
            if (_client == null) return;

            _client.UnregisterHandler<World_Snapshot>(NetEvent.GAME_WORLD_SNAPSHOT, HandleWorldSnapshot);
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendPlayerInput(uint inputTick, in InputState input)
        {
            _client.Send(NetEvent.GAME_PLAYER_INPUT, ProtoUtils.ToPlayerInput(inputTick, input));
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
