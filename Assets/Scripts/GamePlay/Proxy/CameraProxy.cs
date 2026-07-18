using Framework;
using GamePlay.CameraSystem;

namespace GamePlay.Proxy
{
    public class CameraProxy : BaseDataProxy
    {
        public override string DataName => "CameraProxy";
        public override bool IsNeedSaveToLocal => false;

        public CameraConfig Config { get; private set; }

        public override void Load()
        {
            Config = CameraConfig.Instance;
        }

        public override void Clear()
        {
            Config = null;
        }
    }
}
