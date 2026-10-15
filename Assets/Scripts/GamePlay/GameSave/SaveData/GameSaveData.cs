using System.Collections.Generic;

namespace GamePlay.GameSave
{
    /// <summary>
    /// 游戏局内存档抽象
    /// </summary>
    public class GameSaveData
    {
        /// <summary>
        /// 进入过存档的玩家信息
        /// </summary>
        public List<PlayerData> players = new();
    }
}
