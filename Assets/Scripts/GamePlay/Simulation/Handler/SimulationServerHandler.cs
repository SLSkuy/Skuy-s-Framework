using Events;
using Framework;
using GamePlay.Room;
using NetSync;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 玩家输入的主机接收。只解包，入队在主机模拟核。
    /// </summary>
    public sealed class SimulationServerHandler
    {
        private readonly HostSimulationKernel _kernel;
        private RoomManager _room;

        public SimulationServerHandler(HostSimulationKernel kernel)
        {
            _kernel = kernel;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _room = Global.Get<RoomManager>();
            _room.RegisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
        }

        public void Unbind()
        {
            if (_room == null) return;

            _room.UnregisterPlayerHandler<Player_Input>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room = null;
        }

        #endregion

        #region 接收消息

        private void HandlePlayerInput(uint playerId, Player_Input message)
        {
            _kernel.HandlePlayerInput(playerId, message);
        }

        #endregion
    }
}
