using System;
using Core;
using Framework;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;

namespace GamePlay.GameSession
{
    /// <summary>
    /// 玩法粘合点：编排模拟核与关卡控制，不持有名册、不 Admit、不开听。
    /// </summary>
    public sealed class GameManager : SubSystemBase
    {
        private ISimulationKernel _simulationKernel;
        private Action _orchestrationFailed;
        private bool _orchestrationHasFailed;

        #region 属性
        public override int Priority => 200;
        public GameplayPhase Phase { get; private set; } = GameplayPhase.Idle;
        #endregion

        #region 事件
        /// <summary>
        /// 切关或启核失败。流程核作为门闩离开对局；粘合点不反向调用离开。
        /// </summary>
        public event Action OrchestrationFailed
        {
            add
            {
                _orchestrationFailed += value;
                if (_orchestrationHasFailed)
                {
                    value();
                }
            }
            remove => _orchestrationFailed -= value;
        }
        #endregion

        private void ReportOrchestrationFailed()
        {
            if (_orchestrationHasFailed)
            {
                return;
            }

            _orchestrationHasFailed = true;
            _orchestrationFailed?.Invoke();
        }

        /// <summary>
        /// 启动模拟核。可以晚于开听，但不是玩家可感知的第二道门闩。
        /// </summary>
        private bool StartSimulation(SessionRole sessionRole)
        {
            if (_simulationKernel != null)
            {
                return _simulationKernel.IsSessionRunning;
            }

            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = sessionRole == SessionRole.Client ? new ClientSimulationKernel() : new HostSimulationKernel();
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

            Global.Unregister<LocalPawnModule>();
            Global.Unregister<LevelManager>();
            Phase = GameplayPhase.Idle;
        }

        private void HandleLevelLoadCompleted(SceneLoadEvent.CompletedData data)
        {
            if (data.SceneName != GameConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            if (_orchestrationHasFailed)
            {
                return;
            }

            Phase = GameplayPhase.InLevel;
            Global.Get<LocalPawnModule>().HandleMatchLevelCompleted();
        }

        private void HandleLevelLoadFailed(SceneLoadEvent.FailedData data)
        {
            if (data.SceneName != GameConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            ReportOrchestrationFailed();
        }

        #region 子系统生命周期

        public override void Init()
        {
            SessionRole sessionRole = Global.Get<RoomManager>().SessionRole;
            LevelManager levelControl = Global.Register<LevelManager>();
            Phase = GameplayPhase.ChangingLevel;
            levelControl.LoadMatchLevel();
            if (!StartSimulation(sessionRole))
            {
                ReportOrchestrationFailed();
                return;
            }

            Global.Register<LocalPawnModule>();
        }

        public override void BindEvents()
        {
            EventBus.Get<SceneLoadEvent.Completed>().AddListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().AddListener(HandleLevelLoadFailed);
        }

        public override void Destroy()
        {
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().RemoveListener(HandleLevelLoadFailed);
            _orchestrationFailed = null;
            ExitMatch();
        }

        #endregion
    }
}
