using UnityEngine;

namespace Network
{
    /// <summary>
    /// KCP传输配置
    /// </summary>
    [CreateAssetMenu(fileName = "KcpTransportConfig", menuName = "Network/KcpTransportConfig")]
    public class KcpTransportConfig : ScriptableObject
    {
        [Header("KCP传输配置")]
        public uint conv = 1;
        public int mtu = 1400;
        public int sendWindow = 128;
        public int receiveWindow = 128;
        public int updateInterval = 10; 
        public int noDelay = 1;
        public int fastResend = 2;
        public int disableCongestionControl = 1;
    }
}