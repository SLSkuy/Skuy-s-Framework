using Core;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Procedure;
using GamePlay.Room;

namespace GamePlay.GamePlayManager
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
            // 局外数据
            Global.RegisterDataProxy<PlayerInfoDataProxy>();
            Global.RegisterDataProxy<RoomDataProxy>();
            Global.RegisterDataProxy<SimulationDataProxy>();
            
            // 局内数据
            Global.RegisterDataProxy<EntitySpawnConfigProxy>();
            Global.RegisterDataProxy<EntityConfigProxy>();
            Global.RegisterDataProxy<GameSaveDataProxy>();
        }

        private void InitGamePlaySystem()
        {
            Global.Register<RoomManager>();
            Global.Register<ProcedureModule>();
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
