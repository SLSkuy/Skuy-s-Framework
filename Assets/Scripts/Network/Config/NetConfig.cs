using Framework;
using UnityEngine;
using UnityEngine.Serialization;

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
        public float heartBeatStep = 1f;
        public float disconnectTimeout = 10f;

        [Header("KCP设置")]
        public uint kcpConv = 1;
        public int kcpMtu = 1024;
        public int kcpSendWindow = 128;
        public int kcpReceiveWindow = 128;
        public int kcpUpdateInterval = 10;

    }
}
