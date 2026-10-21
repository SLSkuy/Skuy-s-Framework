using System.Collections.Generic;

namespace GamePlay.GameData
{
    /// <summary>
    /// 游戏局内存档抽象
    /// </summary>
    public class GameSaveData
    {
        /// <summary>
        /// 进入过这个存档的玩家
        /// </summary>
        public List<PlayerData> players = new();
    }
}
