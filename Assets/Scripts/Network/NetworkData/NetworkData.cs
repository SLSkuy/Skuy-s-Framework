using UnityEngine;

namespace Network
{
    [CreateAssetMenu(fileName = "NetworkingConfig", menuName = "Networking/Networking Config", order = 1)]
    public class NetworkData : ScriptableObject
    {
        [Header("服务器配置")]
        public string ip = "127.0.0.1";
        public short tcpPort = 11451;
        public short udpPort = 19198;

        [Header("网络属性配置")]
        [Tooltip("网络心跳间隔")] 
        public float heartBeatStep = 1f;
    }
}
