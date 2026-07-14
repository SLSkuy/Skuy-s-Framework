using Framework;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络连接设置
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkingConfig", menuName = "Networking/Networking Config", order = 1)]
    public class NetConfig : ScriptableObjectSingleton<NetConfig>
    {
        [Header("服务器配置")]
        public string ip = "127.0.0.1";
        public short reliablePort = 11451;
        public short fastPort = 19198;

        [Header("网络配置")]
        [Tooltip("心跳发送间隔(秒)")] public float heartBeatStep = 5f;
        [Tooltip("RTT计算间隔(秒)")] public float rttStep = 1f;
        [Tooltip("最大心跳丢失次数，超过后判定断连")] public int maxHeartbeatMisses = 5;
        [Tooltip("传输层超时(秒)，无任何数据接收超过此时间则断连，应 >= heartBeatStep * maxHeartbeatMisses")] public float disconnectTimeout = 20f;

        [Header("KCP设置")]
        public uint kcpConv = 1;
        public int kcpMtu = 1024;
        public int kcpSendWindow = 128;
        public int kcpReceiveWindow = 128;
        public int kcpUpdateInterval = 10;
    }
}
