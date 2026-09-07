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
        public const int LOCAL_CAPACITY = 1;

        private readonly Dictionary<uint, BattlePlayer> _playersById = new();
        private GameManager _gameManager;
        private ISimulationKernel _simulationKernel;

        public BattleRoom(BattleSessionRole sessionRole, bool acceptsRemoteJoin, int capacity = DEFAULT_CAPACITY)
        {
            SessionRole = sessionRole;
            AcceptsRemoteJoin = acceptsRemoteJoin;
            Capacity = capacity > 0 ? capacity : DEFAULT_CAPACITY;
        }

        #region 属性
        public BattleSessionRole SessionRole { get; }
        public int Capacity { get; }
        public uint HostPlayerId { get; private set; }
        public bool AcceptsRemoteJoin { get; }
        public int MemberCount => _playersById.Count;
        #endregion
        
        public uint[] GetPlayerIds()
        {
            uint[] playerIds = new uint[_playersById.Count];
            _playersById.Keys.CopyTo(playerIds, 0);
            return playerIds;
        }

        /// <summary>
        /// 应用远端发来的名单快照
        /// </summary>
        public void ApplyRoster(uint hostPlayerId, IEnumerable<uint> playerIds)
        {
            ClearMembers();
            HostPlayerId = hostPlayerId;
            foreach (uint playerId in playerIds)
            {
                TryAdmit(playerId);
            }
        }

        #region 玩家管理

        /// <summary>
        /// 尝试加入房间
        /// </summary>
        public bool TryAdmit(uint playerId)
        {
            if (_playersById.Count >= Capacity) return false;
            
            BattlePlayer player = new(playerId);
            _playersById[playerId] = player;

            // 若房间还没有房主，则第一个加入的玩家就是房主
            if (HostPlayerId == 0) HostPlayerId = playerId;
            return true;
        }
        
        /// <summary>
        /// 离开战局房间
        /// </summary>
        public void Leave(uint playerId)
        {
            if (_playersById.Remove(playerId, out BattlePlayer player))
            {
                player.State = BattlePlayerState.Left;
            }

            if (HostPlayerId == playerId) HostPlayerId = 0;
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
            HostPlayerId = 0;
        }

        #endregion

        #region 模拟核管理

        /// <summary>
        /// 开战：按会话角色创建模拟核，再登记 GameManager。
        /// </summary>
        public bool StartMatch()
        {
            if (_simulationKernel != null)
            {
                return _simulationKernel.IsSessionRunning;
            }

            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = CreateKernel();
            systems.RegisterSystem(kernel);
            if (!kernel.StartSession())
            {
                systems.UnregisterSystem(kernel);
                return false;
            }

            _simulationKernel = kernel;
            _gameManager = Global.Register<GameManager>();
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
                systems.UnregisterSystem(_gameManager);
                _gameManager = null;
            }

            if (_simulationKernel != null)
            {
                _simulationKernel.StopSession();
                systems.UnregisterSystem(_simulationKernel);
                _simulationKernel = null;
            }
        }

        private ISimulationKernel CreateKernel()
        {
            return SessionRole == BattleSessionRole.Client
                ? new ClientSimulationKernel()
                : new HostSimulationKernel(HostPlayerId,
                    AcceptsRemoteJoin ? EntityObjectRole.Authority : EntityObjectRole.LocalPlay);
        }

        #endregion
    }
}
