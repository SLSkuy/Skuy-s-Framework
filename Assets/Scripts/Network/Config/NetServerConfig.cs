using Framework;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络连接设置
    /// </summary>
    [CreateAssetMenu(fileName = "NetServerConfig", menuName = "Network/NetServerConfig")]
    public class NetServerConfig : ScriptableObjectSingleton<NetServerConfig>
    {
        [Header("服务器端口配置")]
        public short reliablePort = 11451;
        public short fastPort = 19198;

        [Header("客户端连接管理")] 
        public float maxReconnectTime = 20;
    }
}
