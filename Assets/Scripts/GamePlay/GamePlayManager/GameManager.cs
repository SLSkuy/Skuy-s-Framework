using Framework;
using GamePlay.BattleSystem;

namespace GamePlay.GamePlayManager
{
    /// <summary>
    /// 游戏内玩法逻辑调度控制器
    /// </summary>
    public class GameManager
    {
        private BattleManager _battleManager;
        
        #region 游戏进程管理

        /// <summary>
        /// 初始化游戏
        /// </summary>
        public void InitGamePlay()
        {
            _battleManager = Global.Register<BattleManager>();
            _battleManager.InitBattle();
        }
        
        public void DestroyGamePlay()
        {
            _battleManager.StopBattle();
            Global.Register<BattleManager>();
        }
        
        #endregion
    }
}