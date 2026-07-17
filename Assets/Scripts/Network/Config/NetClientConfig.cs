using UnityEngine;
using Framework;

namespace Network
{
    /// <summary>
    /// 客户端网络配置
    /// </summary>
    [CreateAssetMenu(fileName = "NetClientConfig", menuName = "Network/NetClientConfig")]
    public class NetClientConfig : ScriptableObjectSingleton<NetClientConfig>
    {
        [Header("服务器配置")]
        public string ip = "127.0.0.1";
        public short reliablePort = 11451;

        [Header("连接配置")]
        [Tooltip("心跳发送间隔(秒)")] public float heartBeatStep = 5f;
        [Tooltip("RTT计算间隔(秒)")] public float rttStep = 1f;
        [Tooltip("最大连接包体丢失次数，超过后判定断连")] public int maxMissCount = 5;
        
        [Header("重连配置")]
        public bool autoReconnect = true;
        public float reconnectInterval = 5f;
        public int maxReconnectCount = 5;
    }
}