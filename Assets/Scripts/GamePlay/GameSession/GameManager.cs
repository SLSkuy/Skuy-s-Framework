using Framework;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulator;

namespace GamePlay.GameSession
{
    /// <summary>
    /// 玩法粘合点：编排模拟核与关卡控制，不持有名册、不 Admit、不开听。
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        private ISimulationKernel _simulationKernel;
        private SessionRole _sessionRole;

        #region 属性
        public override int Priority => 200;
        public GameplayPhase Phase { get; private set; } = GameplayPhase.Idle;
        #endregion

        /// <summary>
        /// 进入对局时登记玩法粘合点，不加载关卡、不启动模拟核。
        /// </summary>
        public void InitMatch(SessionRole sessionRole)
        {
            _sessionRole = sessionRole;
            Phase = GameplayPhase.Idle;
        }

        /// <summary>
        /// 进入对局流程态：由关卡控制加载关卡，并启动模拟核。
        /// </summary>
        public bool EnterMatch()
        {
            var levelControl = Global.Register<LevelManager>();
            Phase = GameplayPhase.ChangingLevel;
            levelControl.LoadMatchLevel();

            if (!StartSimulation())
            {
                Phase = GameplayPhase.Idle;
                return false;
            }

            Phase = GameplayPhase.InLevel;
            return true;
        }

        /// <summary>
        /// 启动模拟核。可以晚于开听，但不是玩家可感知的第二道门闩。
        /// </summary>
        private bool StartSimulation()
        {
            if (_simulationKernel != null)
            {
                return _simulationKernel.IsSessionRunning;
            }

            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = _sessionRole == SessionRole.Client
                ? new ClientSimulationKernel()
                : new HostSimulationKernel();
            systems.RegisterSystem(kernel);
            if (!kernel.StartSession())
            {
                systems.UnregisterSystem(kernel);
                return false;
            }

            _simulationKernel = kernel;
            return true;
        }

        /// <summary>
        /// 结束玩法编排并拆掉模拟核与关卡控制。
        /// </summary>
        private void ExitMatch()
        {
            SystemManager systems = Global.Get<SystemManager>();
            if (_simulationKernel != null)
            {
                _simulationKernel.StopSession();
                systems.UnregisterSystem(_simulationKernel);
                _simulationKernel = null;
            }

            Global.Unregister<LevelManager>();
            Phase = GameplayPhase.Idle;
        }

        #region 子系统生命周期

        public override void Destroy()
        {
            ExitMatch();
        }

        #endregion
    }
}
