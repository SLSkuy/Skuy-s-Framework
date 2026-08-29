using System;
using System.Collections.Generic;
using Events;
using NetSync;
using Network;

namespace GamePlay.Battle
{
    /// <summary>
    /// 一场战局的玩家名册。不持有传输、不生成角色、不广播世界快照。
    /// </summary>
    public sealed class BattleSession : IDisposable
    {
        public const int DefaultMaxPlayers = 8;

        private readonly IConnectionEvents _connectionEvents;
        private readonly IMatchMessenger _messenger;
        private readonly Dictionary<uint, BattlePlayer> _playersById = new();
        private readonly Dictionary<uint, uint> _playerIdByClientId = new();
        private uint _nextPlayerId = 1;
        private int _worldSnapshotBroadcastCount;

        public BattleSession(IConnectionEvents connectionEvents, IMatchMessenger messenger,
            int maxPlayers = DefaultMaxPlayers)
        {
            _connectionEvents = connectionEvents;
            _messenger = messenger;
            MaxPlayers = maxPlayers > 0 ? maxPlayers : DefaultMaxPlayers;
            _connectionEvents.ClientRemoved += HandleClientRemoved;
        }

        #region 属性
        public int MaxPlayers { get; }
        public int PlayerCount => _playersById.Count;
        public int WorldSnapshotBroadcastCount => _worldSnapshotBroadcastCount;
        #endregion

        /// <summary>
        /// 连接加入战局。成功时分配独立 playerId，并通过发送门面回加入结果。
        /// </summary>
        public bool TryJoin(uint clientId, out uint playerId)
        {
            playerId = 0;
            if (clientId == 0)
            {
                SendJoinResponse(clientId, false);
                return false;
            }

            if (_playerIdByClientId.ContainsKey(clientId))
            {
                SendJoinResponse(clientId, false);
                return false;
            }

            if (_playersById.Count >= MaxPlayers)
            {
                SendJoinResponse(clientId, false);
                return false;
            }

            playerId = _nextPlayerId++;
            BattlePlayer player = new(playerId, clientId);
            _playersById[playerId] = player;
            _playerIdByClientId[clientId] = playerId;
            SendJoinResponse(clientId, true);
            return true;
        }

        /// <summary>
        /// 拒绝加入并经发送门面回包，不改名册。
        /// </summary>
        public void RejectJoin(uint clientId)
        {
            SendJoinResponse(clientId, false);
        }

        public bool TryGetPlayer(uint playerId, out BattlePlayer player)
        {
            return _playersById.TryGetValue(playerId, out player);
        }

        public bool TryGetPlayerByClient(uint clientId, out BattlePlayer player)
        {
            player = null;
            if (!_playerIdByClientId.TryGetValue(clientId, out uint playerId)) return false;
            return _playersById.TryGetValue(playerId, out player);
        }

        public void Dispose()
        {
            _connectionEvents.ClientRemoved -= HandleClientRemoved;
            _playersById.Clear();
            _playerIdByClientId.Clear();
        }

        private void HandleClientRemoved(uint clientId)
        {
            if (!_playerIdByClientId.Remove(clientId, out uint playerId)) return;
            if (_playersById.Remove(playerId, out BattlePlayer player))
            {
                player.Phase = BattlePlayerPhase.Left;
            }
        }

        private void SendJoinResponse(uint clientId, bool accepted)
        {
            if (_messenger == null || clientId == 0) return;
            Game_Join_Response response = new() { Accepted = accepted };
            _messenger.SendReliable(clientId, NetEvent.GAME_JOIN_RESPONSE, response);
        }
    }
}
