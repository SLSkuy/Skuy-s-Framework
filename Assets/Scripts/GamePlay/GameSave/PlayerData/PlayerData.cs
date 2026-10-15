using System;
using GamePlay.Room;
using NetSync;

namespace GamePlay.GameSave
{
    /// <summary>
    /// 玩家数据实例
    /// </summary>
    public class PlayerData
    {
        /// <summary>
        /// 玩家唯一实例ID，首次运行时创建本机玩家UUID
        /// </summary>
        public Guid uuid;

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string playerName = "DefaultPlayerName";

        public PlayerData() { }

        public PlayerData(Player player)
        {
            uuid = player.UUID;
            playerName = player.PlayerName;
        }

        public void UpdateInfo(PlayerData data)
        {
            // TODO: 后续添加额外的信息更新
            playerName = data.playerName;
        }

        public PlayerDataProto ToProto()
        {
            return new PlayerDataProto
            {
                UUID = uuid.ToString(),
                PlayerName = playerName,
            };
        }
    }
}