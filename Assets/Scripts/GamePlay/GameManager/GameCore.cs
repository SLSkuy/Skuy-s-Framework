using Core;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Room;

namespace GamePlay.GameManager
{
    /// <summary>
    /// 玩法启动根，桥接框架层，注册游戏业务逻辑驱动器
    /// </summary>
    public class GameCore : MonoSingleton<GameCore>
    {
        private void SetUpGameCore()
        {
            InitGamePlayDataProxy();
            InitGamePlaySystem();
        }

        private void InitGamePlayDataProxy()
        {
            Global.RegisterDataProxy<EntityConfigProxy>();
            Global.RegisterDataProxy<GameSaveDataProxy>();
            Global.RegisterDataProxy<PlayerDataProxy>();
            Global.RegisterDataProxy<RoomDataProxy>();
        }

        private void InitGamePlaySystem()
        {
            Global.Register<RoomManager>();
        }
        
        #region 生命周期

        protected override void BindEvents()
        {
            AppCore.OnAppReady += SetUpGameCore;
            AppCore.OnAppQuit += ShutDown;
        }

        protected override void UnbindEvents()
        {
            AppCore.OnAppReady -= SetUpGameCore;
            AppCore.OnAppQuit -= ShutDown;
        }

        #endregion
    }
}
