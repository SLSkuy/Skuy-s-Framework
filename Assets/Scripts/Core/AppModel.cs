using System;

namespace Core
{
    /// <summary>
    /// 全局游戏配置
    /// </summary>
    public class AppModel
    {
        #region 性能设置
        public int FrameLimit = 60;
        #endregion

        #region 分辨率设置
        public int ScreenWidth = 1920;
        public int ScreenHeight = 1080;
        #endregion
        
        #region 游戏统计
        public DateTime LastPlayTime;
        #endregion
    }
}