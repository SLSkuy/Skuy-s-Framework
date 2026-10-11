using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络连接设置
    /// </summary>
    [CreateAssetMenu(fileName = "NetServerConfig", menuName = "Network/NetServerConfig")]
    public class NetServerConfig : ScriptableObject
    {
        [Header("服务器端口配置")]
        public short reliablePort = 11451;
        public short fastPort = 19198;

        [Header("客户端连接管理")] 
        public float rttStep = 1f; 
        public float maxReconnectTime = 20;

        [Header("快速通道未绑定会话")]
        [Tooltip("尚未通过 token 绑定的 KCP 会话，从创建起超过该秒数即关闭。与断线重连宽限期无关。")]
        public float unboundFastSessionSeconds = 2f;
    }
}
