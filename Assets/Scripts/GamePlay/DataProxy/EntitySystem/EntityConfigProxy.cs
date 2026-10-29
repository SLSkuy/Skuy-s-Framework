using Framework;
using GamePlay.BattleSystem;
using YooAsset;

namespace GamePlay.DataProxy
{
    public class EntityConfigProxy : BaseDataProxy
    {
        public override string DataPath => "Config_EntityConfig";
        public override bool IsNeedSaveToLocal => false;
        
        private AssetHandle _asset;

        #region 属性
        public CharacterConfig Config;
        #endregion
        
        public override void Load()
        {
            _asset = Global.Load<CharacterConfig>(DataPath);
            Config = _asset.AssetObject as CharacterConfig;
        }

        public override void Destroy()
        {
            _asset.Dispose();
        }
    }
}