using System;

namespace Network
{
    /// <summary>
    /// KCP传输设置
    /// </summary>
    [Serializable]
    public sealed class TransportSettings
    {
        public uint conv = 1;
        public int mtu = 1400;
        public int sendWindow = 128;
        public int receiveWindow = 128;
        public int updateInterval = 10; 
        public int noDelay = 1;
        public int fastResend = 2;
        public int disableCongestionControl = 1;
        public float disconnectTimeout = 10f;

        public static TransportSettings Default => new TransportSettings();

        public static TransportSettings FromConfig(NetConfig config)
        {
            if (config == null)
            {
                return Default;
            }

            return new TransportSettings
            {
                conv = config.kcpConv,
                mtu = config.kcpMtu,
                sendWindow = config.kcpSendWindow,
                receiveWindow = config.kcpReceiveWindow,
                updateInterval = config.kcpUpdateInterval
            };
        }
    }
}
