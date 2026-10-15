using System;
using GamePlay.GameSave;

namespace GamePlay.Room
{
    /// <summary>
    /// 对局中的玩家实例，只存在于对剧中
    /// </summary>
    public class Player
    {
        #region 属性
        public Guid UUID { get; }
        public string PlayerName { get; private set; }
        public PlayerState State { get; private set; }
        #endregion

        public Player(Guid uuid, string playerName)
        {
            UUID = uuid;
            PlayerName = playerName;
            State = PlayerState.Left;
        }

        public Player(PlayerData playerData)
        {
            UUID = playerData.uuid;
            PlayerName = playerData.playerName;
            State = PlayerState.Left;
        }

        #region 玩家状态管理

        /// <summary>
        /// 标记玩家处于加入状态
        /// </summary>
        public void Joining()
        {
            State = PlayerState.Joining;
        }

        /// <summary>
        /// 标记玩家实例已完成连接，正常加入游戏
        /// </summary>
        public void Active()
        {
            State = PlayerState.Active;
        }

        /// <summary>
        /// 标记玩家已经退出战局
        /// </summary>
        public void Inactive()
        {
            State = PlayerState.Left;
        }

        #endregion
    }
}
