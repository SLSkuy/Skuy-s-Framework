using System;

namespace Core
{
    /// <summary>
    /// 全局游戏数据（存档、玩家进度等）
    /// </summary>
    [Serializable]
    public class AppModel
    {
        #region 玩家基础数据
        public string playerName;
        #endregion

        #region 游戏统计
        public DateTime LastPlayTime;
        #endregion

        public AppModel()
        {
            playerName = "Player";
            LastPlayTime = DateTime.Now;
        }
    }
}