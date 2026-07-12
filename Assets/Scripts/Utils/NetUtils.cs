using System;
using System.Buffers.Binary;
using Google.Protobuf;
using NetConnect;

namespace Network
{
    /// <summary>
    /// 网络传输工具
    /// </summary>
    public static class NetUtils
    {
        // 31                     16 15                     0
        // +-----------------------+------------------------+
        // | Flag                  | MessageId             |
        // +-----------------------+------------------------+

        /// <summary>
        /// 协议头长度
        /// </summary>
        private const int HEADER_SIZE = sizeof(uint);

        #region 序列化

        /// <summary>
        /// 客户端协议序列化
        /// </summary>
        public static byte[] Proto2Bytes(NetEvent evt, IMessage msg)
        {
            ushort messageId = (ushort)evt;
            byte[] body = msg.ToByteArray();

            // 后续接入消息flag
            ushort flag = 0;

            uint header = ((uint)flag << 16) | messageId;
            byte[] packet = new byte[HEADER_SIZE + body.Length];

            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(), header);
            Buffer.BlockCopy(body, 0, packet, HEADER_SIZE, body.Length);

            return packet;
        }

        #endregion

        #region 反序列化

        /// <summary>
        /// 字节流反序列化
        /// </summary>
        public static (NetEvent, IMessage) Bytes2Proto(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HEADER_SIZE) return (NetEvent.ERROR, null);
            uint header = BinaryPrimitives.ReadUInt32LittleEndian(bytes);

            ushort messageId = (ushort)(header & 0xFFFF);
            ReadOnlySpan<byte> body = bytes.AsSpan(HEADER_SIZE);
            
            return ((NetEvent)messageId, ParseNetMessage((NetEvent)messageId, body));
        }

        #endregion

        #region Parser

        private static IMessage ParseNetMessage(NetEvent evt, ReadOnlySpan<byte> body)
        {
            switch (evt)
            {
                case NetEvent.RELIABLE_CONNECT_REQUEST:
                    return Client_Reliable_Connect_Request.Parser.ParseFrom(body);
                
                case NetEvent.RELIABLE_CONNECT_RESPONSE:
                    return Client_Reliable_Connect_Response.Parser.ParseFrom(body);
                
                case NetEvent.FAST_CONNECT_REQUEST:
                    return Client_Fast_Connect_Request.Parser.ParseFrom(body);

                case NetEvent.PING:
                    return Ping.Parser.ParseFrom(body);
                
                case NetEvent.PONG:
                    return Pong.Parser.ParseFrom(body);

                case NetEvent.CHAT_TEST:
                    return Chat_Test.Parser.ParseFrom(body);

                default:
                    return null;
            }
        }

        #endregion
    }
}