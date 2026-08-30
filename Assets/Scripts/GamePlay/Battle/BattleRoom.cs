using System.Collections.Generic;
using Framework;
using GamePlay.GameSession;

namespace GamePlay.Battle
{
    /// <summary>
    /// 游戏房间：跨关卡存活的聚会。开战之后持有 GameManager 与模拟核。
    /// </summary>
    public sealed class BattleRoom
    {
        public const int DEFAULT_CAPACITY = 4;

        private readonly Dictionary<uint, BattlePlayer> _playersById = new();
        private GameManager _gameManager;

        public BattleRoom(bool acceptsRemoteJoin, int capacity = DEFAULT_CAPACITY)
        {
            AcceptsRemoteJoin = acceptsRemoteJoin;
            Capacity = capacity > 0 ? capacity : DEFAULT_CAPACITY;
        }

        #region 属性
        public int Capacity { get; }
        public bool AcceptsRemoteJoin { get; }
        public uint HostPlayerId { get; private set; }
        #endregion

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
        /// 开战：创建并登记 GameManager 与权威模拟核，按当时名册生成实体。
        /// </summary>
        public bool StartMatch()
        {
            _gameManager = Global.Register<GameManager>();

            return true;
        }

        /// <summary>
        /// 结束对局：拆掉流程对象与模拟核，名册保留。
        /// </summary>
        public void EndMatch()
        {
            Global.Unregister(_gameManager);
            _gameManager = null;
        }

        #endregion
    }
}
