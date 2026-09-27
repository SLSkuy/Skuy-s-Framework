using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.Room;
using NetSync;
using Utils;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 主机模拟收发：接收玩家输入，广播世界快照。只组包和解包。
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

        #region 发送消息

        public void BroadcastWorldSnapshot(uint snapshotTick, List<PlayerProcessedSnapshot> samples)
        {
            World_Snapshot message = new World_Snapshot
            {
                SnapshotTick = snapshotTick,
            };
            foreach (var sample in samples)
            {
                message.PlayerSnapshots.Add(ProtoUtils.ToPlayerSnapshot(sample.entityId, sample.lastProcessedInputTick, sample.state));
            }

            _room.Broadcast(NetEvent.GAME_WORLD_SNAPSHOT, message);
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
