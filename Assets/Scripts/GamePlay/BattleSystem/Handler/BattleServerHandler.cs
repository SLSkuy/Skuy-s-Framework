using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.Room;
using NetSync;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleServerHandler
    {
        private readonly BattleManager _battleManager;
        private RoomManager _room;

        public BattleServerHandler(BattleManager battleManager)
        {
            _battleManager = battleManager;
        }
        
        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _room = Global.Get<RoomManager>();
            _room.RegisterPlayerHandler<Player_Inputs>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
        }

        public void Unbind()
        {
            if (_room == null) return;

            _room.UnregisterPlayerHandler<Player_Inputs>(NetEvent.GAME_PLAYER_INPUT, HandlePlayerInput);
            _room = null;
        }
        
        #endregion
        
        #region 发送消息
        
        public void BroadcastWorldSnapshot(uint snapshotTick, List<PlayerProcessedSnapshot> samples)
        {
            World_Snapshot message = new World_Snapshot { SnapshotTick = snapshotTick };
            foreach (var sample in samples)
            {
                message.PlayerSnapshots.Add(sample.snapshot.ToProto(sample.entityId, sample.lastProcessedInputTick));
            }

            uint[] activeIDs = _room.GetAllPlayerConnectionID();
            _room.SendGroup(activeIDs, NetEvent.GAME_WORLD_SNAPSHOT, message);
        }

        #endregion

        #region 接收消息
        
        private void HandlePlayerInput(Guid playerId, Player_Inputs message)
        {
            _battleManager.HandlePlayerInput(playerId, message);
        }

        #endregion
    }
}