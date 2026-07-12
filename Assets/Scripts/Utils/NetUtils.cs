using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

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

        private static readonly Dictionary<NetEvent, MessageParser> Parsers = new();

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
            NetEvent evt = (NetEvent)messageId;

            if (!Parsers.TryGetValue(evt, out var parser))
            {
                return (NetEvent.ERROR, null);
            }

            ReadOnlySpan<byte> body = bytes.AsSpan(HEADER_SIZE);
            return (evt, parser.ParseFrom(body));
        }

        #endregion

        #region Parser注册

        /// <summary>
        /// 注册消息类型的Protobuf Parser，用于反序列化
        /// </summary>
        public static void RegisterParser(NetEvent evt, MessageParser parser)
        {
            if (Parsers.TryAdd(evt, parser))
            {
                Debug.Log($"[Net] 注册事件 {evt}");   
            }
        }

        #endregion
    }
}