using Framework;
using GamePlay.EntitySystem;
using YooAsset;

namespace GamePlay.DataProxy
{
    public class EntityConfigProxy : BaseDataProxy
    {
        public override string DataPath => "Config_EntityConfig";
        public override bool IsNeedSaveToLocal => false;
        
        private AssetHandle _asset;

        #region 属性
        public EntityConfig Config;
        #endregion
        
        public override void Load()
        {
            _asset = Global.Load<EntityConfig>(DataPath);
            Config = _asset.AssetObject as EntityConfig;
        }

        public override void Destroy()
        {
            _asset.Dispose();
        }
    }
}