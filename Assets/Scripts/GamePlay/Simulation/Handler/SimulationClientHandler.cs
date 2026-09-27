using Events;
using Framework;
using Network;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家输入的客户端发送。只组包，采样与节拍在客户端模拟核。
    /// </summary>
    public sealed class SimulationClientHandler
    {
        private NetClient _client;

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
        }

        public void Unbind()
        {
            _client = null;
        }

        #endregion

        #region 发送消息

        public void SendPlayerInput(uint inputTick, in InputState input)
        {
            _client.Send(NetEvent.GAME_PLAYER_INPUT, ProtoUtils.ToPlayerInput(inputTick, input));
        }

        #endregion
    }
}
