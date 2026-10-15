using System;
using NetSync;

namespace GamePlay.Room
{
    /// <summary>
    /// 房间配置
    /// </summary>
    public class RoomConfig
    {
        public Guid hostUUID;
        public bool acceptRemoteJoin;
        public int capacity;
        
        public RoomConfig Clone()
        {
            return new RoomConfig
            {
                hostUUID = hostUUID,
                acceptRemoteJoin = acceptRemoteJoin,
                capacity = capacity
            };
        }

        public RoomConfigProto ToProto()
        {
            return new RoomConfigProto
            {
                HostUUID = hostUUID.ToString(),
                AcceptRemoteJoin = acceptRemoteJoin,
                Capacity = capacity
            };
        }
    }
}