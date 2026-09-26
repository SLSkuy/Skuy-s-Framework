using Events;
using Framework;
using GamePlay.Room;
using NetSync;
using Network;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家输入的主机接收。只解包，入队在主机模拟核。
    /// </summary>
    public sealed class SimulationServerHandler
    {
        private readonly HostSimulationKernel _kernel;
        private NetServer _server;

        public SimulationServerHandler(HostSimulationKernel kernel)
        {
            _kernel = kernel;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _server = Global.Get<NetServer>();
            _server.RegisterHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
        }

        public void Unbind()
        {
            if (_server == null) return;

            _server.UnregisterHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _server = null;
        }

        #endregion

        #region 接收消息

        private void HandlePlayerInput(uint connectionId, Player_Input message)
        {
            if (!Global.Get<RoomManager>().TryGetPlayerId(connectionId, out uint playerId)) return;
            _kernel.HandlePlayerInput(playerId, message);
        }

        #endregion
    }
}
