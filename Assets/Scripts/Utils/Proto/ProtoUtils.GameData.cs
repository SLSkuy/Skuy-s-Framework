using System;
using GamePlay.GameData;
using GamePlay.Room;
using NetSync;

namespace Utils
{
    /// <summary>
    /// 游戏相关数据网络协议转换定义
    /// </summary>
    public static partial class ProtoUtils
    {
        public static PlayerInfoData ToData(this PlayerInfoDataProto player)
        {
            return new PlayerInfoData
            {
                uuid = Guid.Parse(player.UUID),
                playerName = player.PlayerName,
            };
        }

        public static PlayerInfoDataProto ToProto(this PlayerInfoData playerInfo)
        {
            return new PlayerInfoDataProto
            {
                UUID = playerInfo.uuid.ToString(),
                PlayerName = playerInfo.playerName,
            };
        }
        
        public static RoomConfig ToData(this RoomConfigProto config)
        {
            return new RoomConfig
            {
                hostUUID = Guid.Parse(config.HostUUID),
                acceptRemoteJoin = config.AcceptRemoteJoin,
                capacity = config.Capacity,
            };
        }

        public static RoomConfigProto ToProto(this RoomConfig config)
        {
            return new RoomConfigProto
            {
                HostUUID = config.hostUUID.ToString(),
                AcceptRemoteJoin = config.acceptRemoteJoin,
                Capacity = config.capacity,
            };
        }
    }
}