using Framework;
using GamePlay.Room;
using GamePlay.Simulation;

namespace GamePlay.GamePlayManager
{
    /// <summary>
    /// 游戏内玩法逻辑调度控制器
    /// </summary>
    public class GameManager
    {
        private ISimulationKernel _kernel;
        private RoomManager _room;

        #region 游戏进程管理

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void InitGamePlay()
        {
            _room = Global.Get<RoomManager>();
            
            InitSimulationKernel();
        }
        
        public void DestroyGamePlay()
        {
            DestroySimulationKernel();
            
            _room = null;
        }
        
        #endregion
        
        /// <summary>
        /// 初始化网络同步模拟核
        /// </summary>
        private void InitSimulationKernel()
        {
            SessionRole role = _room.Role;
            _kernel = role == SessionRole.Host ? Global.Register<HostSimulationKernel>() 
                : Global.Register<ClientSimulationKernel>();
            _kernel.StartSession();
        }
        
        private void DestroySimulationKernel()
        {
            _kernel?.StopSession();
            _kernel = null;
            
            SessionRole role = _room.Role;
            if (role == SessionRole.Host) Global.Unregister<HostSimulationKernel>();
            else Global.Unregister<ClientSimulationKernel>();
        }
    }
}