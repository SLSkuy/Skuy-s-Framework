using System;
using System.Collections.Generic;

namespace GamePlay.GameData
{
    /// <summary>
    /// 游戏局内存档抽象
    /// </summary>
    public class GameSaveData
    {
        /// <summary>
        /// 进入过存档的玩家信息
        /// </summary>
        public List<PlayerInfoData> playerInfos = new();
        
        /// <summary>
        /// 玩家实体数据，通过UUID索引
        /// </summary>
        public Dictionary<Guid, PlayerEntityData> playerEntities = new();
    }
}
