using System;

namespace GamePlay.GameData
{
    /// <summary>
    /// 玩家信息数据示例，与玩家实体数据相互独立
    /// 通过UUID索引玩家的局内数据
    /// </summary>
    public class PlayerInfoData
    {
        /// <summary>
        /// 玩家唯一实例ID，首次运行时创建本机玩家UUID
        /// </summary>
        public Guid uuid;

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string playerName = "DefaultPlayerName";
    }
}
