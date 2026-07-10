using System;

namespace Network
{
    [Serializable]
    public sealed class TransportSettings
    {
        public uint Conv = 1;
        public int Mtu = 1400;
        public int SendWindow = 128;
        public int ReceiveWindow = 128;
        public int UpdateInterval = 10;
        public int NoDelay = 1;
        public int FastResend = 2;
        public int DisableCongestionControl = 1;
        public float DisconnectTimeout = 10f;

        public static TransportSettings Default => new TransportSettings();

        public static TransportSettings FromConfig(NetConfig config)
        {
            if (config == null)
            {
                return Default;
            }

            return new TransportSettings
            {
                Conv = config.kcpConv,
                Mtu = config.kcpMtu,
                SendWindow = config.kcpSendWindow,
                ReceiveWindow = config.kcpReceiveWindow,
                UpdateInterval = config.kcpUpdateInterval
            };
        }
    }
}
