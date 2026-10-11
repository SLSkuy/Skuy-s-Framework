using System;
using Framework;

namespace Core
{
    /// <summary>
    /// 全局游戏数据代理。启动时从本地 JSON 读取，销毁时写回。
    /// </summary>
    public class AppModelProxy : BaseDataProxy
    {
        public override string DataPath => "AppModel";
        public override bool IsNeedSaveToLocal => true;
        
        private AppModel _model;
        
        #region 数据管理方法
                
        public override void Load()
        {
            if (Global.LoadJson<AppModel>(DataPath, out var data))
            {
                _model = data;
                return;
            }
            
            _model = new AppModel();
        }

        public override void Save()
        {
            _model.LastPlayTime = DateTime.Now;
            
            Global.SaveJson(DataPath, _model);
        }

        #endregion
    }
}
