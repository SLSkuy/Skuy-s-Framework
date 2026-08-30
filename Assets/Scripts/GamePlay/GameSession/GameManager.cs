using Framework;
using GamePlay.Simulator;

namespace GamePlay
{
    /// <summary>
    /// 游戏管理器，管理整个游戏的生命周期
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        private LocalSimulationHost _localHost;

        #region 属性
        public override int Priority => 200;
        public GameplayState Phase { get; private set; } = GameplayState.Idle;
        #endregion

        /// <summary>
        /// 开始单机一局。进行中再次调用失败。
        /// </summary>
        public bool StartLocal()
        {
            if (Phase == GameplayState.InPlay) return false;
            if (Phase == GameplayState.Ending) return false;

            _localHost = new LocalSimulationHost();
            Global.Register(_localHost);
            
            if (!_localHost.StartSession()) return false;

            Phase = GameplayState.InPlay;
            return true;
        }

        /// <summary>
        /// 结束当前玩法会话并回到空闲。暂停应用状态不会调用此方法。
        /// </summary>
        public void Stop()
        {
            if (Phase == GameplayState.Idle) return;

            Phase = GameplayState.Ending;
            _localHost?.StopSession();
            Phase = GameplayState.Idle;
        }

        #region 生命周期

        public override void Destroy()
        {
            Stop();
        }

        #endregion
    }
}
