using GamePlay.BattleSystem;

namespace GamePlay.GameData
{
    /// <summary>
    /// 玩家数据集合，聚合玩家身份信息/实体信息
    /// </summary>
    public class PlayerData
    {
        /// <summary>
        /// 玩家身份，uuid 只记在这里
        /// </summary>
        public PlayerInfoData info = new();

        /// <summary>
        /// 该玩家在这一局里的出场进度
        /// </summary>
        public EntitySnapshot Snapshot = new();
    }
}
