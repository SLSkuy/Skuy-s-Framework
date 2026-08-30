using System.Collections.Generic;
using Framework;
using GamePlay.GameSession;
using GamePlay.Simulator;

namespace GamePlay.Battle
{
    /// <summary>
    /// 游戏房间：跨关卡存活的聚会。开战之后持有 GameManager 与模拟核。
    /// </summary>
    public sealed class BattleRoom
    {
        public const int DEFAULT_CAPACITY = 4;

        private readonly Dictionary<uint, BattlePlayer> _playersById = new();
        private readonly Dictionary<uint, uint> _playerIdByConnectionId = new();
        private GameManager _gameManager;
        private LocalSimulationHost _simulationHost;
        private uint _nextPlayerId = 1;

        public BattleRoom(uint roomId, int capacity = DEFAULT_CAPACITY)
        {
            RoomId = roomId;
            Capacity = capacity > 0 ? capacity : DEFAULT_CAPACITY;
        }

        #region 属性
        public uint RoomId { get; }
        public int Capacity { get; }
        public uint HostPlayerId { get; private set; }
        public int MemberCount => _playersById.Count;
        public GameManager GameManager => _gameManager;
        public LocalSimulationHost SimulationHost => _simulationHost;
        public bool HasMatch => _gameManager != null;
        public string PlaySceneName { get; set; }
        #endregion

        /// <summary>
        /// 尝试加入房间，返回分配的玩家ID
        /// </summary>
        public bool TryAdmit(uint connectionId, out uint playerId)
        {
            playerId = 0;
            if (_playerIdByConnectionId.ContainsKey(connectionId)) return false;
            if (_playersById.Count >= Capacity) return false;

            playerId = _nextPlayerId++;
            BattlePlayer player = new(playerId, connectionId);
            _playersById[playerId] = player;
            _playerIdByConnectionId[connectionId] = playerId;

            // 若房间还没有房主，则第一个加入的玩家就是房主
            if (HostPlayerId == 0) HostPlayerId = playerId;
            return true;
        }

        /// <summary>
        /// 通过连接ID获取玩家实体
        /// </summary>
        public bool TryGetPlayerByConnection(uint connectionId, out BattlePlayer player)
        {
            player = null;
            if (!_playerIdByConnectionId.TryGetValue(connectionId, out uint playerId)) return false;
            return _playersById.TryGetValue(playerId, out player);
        }

        /// <summary>
        /// 离开战局房间
        /// </summary>
        public bool Leave(uint connectionId)
        {
            if (!_playerIdByConnectionId.Remove(connectionId, out uint playerId)) return false;
            if (_playersById.Remove(playerId, out BattlePlayer player))
            {
                player.State = BattlePlayerState.Left;
            }

            if (HostPlayerId == playerId) HostPlayerId = 0;
            return true;
        }

        /// <summary>
        /// 开战：创建并登记 GameManager 与权威模拟核，按当时名册生成实体。
        /// </summary>
        public bool StartMatch()
        {
            if (HasMatch) return false;
            if (MemberCount == 0) return false;

            SystemManager systems = Global.Get<SystemManager>();
            LocalSimulationHost host = new();
            systems.RegisterSystem(host);
            if (!host.StartSession())
            {
                systems.UnregisterSystem(host);
                return false;
            }

            GameManager gameManager = new(this, host);
            systems.RegisterSystem(gameManager);
            gameManager.BeginMatch();
            _simulationHost = host;
            _gameManager = gameManager;
            return true;
        }

        /// <summary>
        /// 结束对局：拆掉流程对象与模拟核，名册保留。
        /// </summary>
        public void EndMatch()
        {
            SystemManager systems = Global.Get<SystemManager>();
            if (_gameManager != null)
            {
                _gameManager.NotifyMatchEnded();
                systems.UnregisterSystem(_gameManager);
                _gameManager = null;
            }

            if (_simulationHost != null)
            {
                _simulationHost.StopSession();
                systems.UnregisterSystem(_simulationHost);
                _simulationHost = null;
            }
        }

        /// <summary>
        /// 清空战局房间内的玩家
        /// </summary>
        public void ClearMembers()
        {
            foreach (BattlePlayer player in _playersById.Values)
            {
                player.State = BattlePlayerState.Left;
            }

            _playersById.Clear();
            _playerIdByConnectionId.Clear();
            HostPlayerId = 0;
        }
    }
}
