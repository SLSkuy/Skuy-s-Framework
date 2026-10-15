using System;
using Framework;
using GamePlay.Room;
using YooAsset;

namespace GamePlay.DataProxy
{
    public class RoomDataProxy : BaseDataProxy
    {
        public override string DataPath => "Config_RoomConfig";
        public override bool IsNeedSaveToLocal => false;

        private AssetHandle _handle;
        private RoomConfig _defaultConfig;
        private RoomConfig _curConfig;

        // 当前应用的房间配置
        #region 属性
        public RoomConfig CurrentConfig => _curConfig;

        public Guid HostPlayerUUID => _curConfig.hostUUID;
        public bool AcceptRemoteJoin => _curConfig.acceptRemoteJoin;
        public int LocalPlayCapacity => 1;
        public int MultiPlayCapacity => _curConfig.capacity;
        #endregion

        /// <summary>
        /// 更新房间设置
        /// </summary>
        public void UpdateConfig(RoomConfig config)
        {
            if (config == null) return;
            _curConfig = config.Clone();
        }

        /// <summary>
        /// 获取默认房间配置
        /// </summary>
        public RoomConfig GetDefaultConfig()
        {
            return _defaultConfig.Clone();
        }

        #region 数据管理方法

        public override void Load()
        {
            _handle = Global.Load<RoomConfigSO>(DataPath);
            RoomConfigSO asset = _handle.AssetObject as RoomConfigSO;
            
            _defaultConfig = new RoomConfig
            {
                hostUUID = Guid.Empty,
                acceptRemoteJoin = asset.acceptRemoteJoin,
                capacity = asset.capacity
            };

            _curConfig = _defaultConfig.Clone();
        }

        public override void Destroy()
        {
            _curConfig = null;
            _defaultConfig = null;

            _handle?.Dispose();
            _handle = null;
        }

        #endregion
    }
}