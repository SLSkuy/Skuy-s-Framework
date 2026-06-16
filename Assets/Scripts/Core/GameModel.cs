using System;

namespace Core
{
    /// <summary>
    /// 全局游戏数据（存档、玩家进度等）
    /// </summary>
    [Serializable]
    public class GameModel
    {
        #region 玩家基础数据
        public string playerName;
        #endregion

        #region 游戏统计
        public DateTime LastPlayTime;
        #endregion

        public GameModel()
        {
            playerName = "Player";
            LastPlayTime = DateTime.Now;
        }
    }
}