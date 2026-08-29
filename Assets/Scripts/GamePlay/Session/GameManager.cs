using Core;
using Framework;
using GamePlay.Simulator;

namespace GamePlay
{
    /// <summary>
    /// 玩法会话编排器：启动/停止单机 Host，不管理菜单加载暂停，不拉起网络。
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        private LocalSimulationHost _localHost;

        #region 属性
        public override int Priority => 200;
        public GameplaySessionPhase Phase { get; private set; } = GameplaySessionPhase.Idle;
        public bool IsInPlay => Phase == GameplaySessionPhase.InPlay;
        #endregion

        /// <summary>
        /// 开始单机一局。进行中再次调用失败。
        /// </summary>
        public bool StartLocal()
        {
            if (Phase == GameplaySessionPhase.InPlay) return false;
            if (Phase == GameplaySessionPhase.Ending) return false;

            _localHost = GetOrRegisterLocalHost();
            if (_localHost == null) return false;
            if (!_localHost.StartSession()) return false;

            Phase = GameplaySessionPhase.InPlay;
            return true;
        }

        /// <summary>
        /// 结束当前玩法会话并回到空闲。暂停应用状态不会调用此方法。
        /// </summary>
        public void Stop()
        {
            if (Phase == GameplaySessionPhase.Idle) return;

            Phase = GameplaySessionPhase.Ending;
            _localHost?.StopSession();
            Phase = GameplaySessionPhase.Idle;
        }

        #region 子系统生命周期

        public override void Destroy()
        {
            Stop();
        }

        #endregion

        private static LocalSimulationHost GetOrRegisterLocalHost()
        {
            SystemManager systemManager = GameCore.Instance != null
                ? GameCore.Instance.SystemMgr
                : Global.Get<SystemManager>();
            if (systemManager == null) return Global.Get<LocalSimulationHost>();
            return systemManager.GetSystem<LocalSimulationHost>() ??
                systemManager.RegisterSystem<LocalSimulationHost>();
        }
    }
}
