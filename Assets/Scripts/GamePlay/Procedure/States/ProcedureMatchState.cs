using Core;
using Framework;
using GamePlay.GameCore;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：进入时登记关卡控制、模拟核与本机 pawn；离开时拆除。
    /// </summary>
    public sealed class ProcedureMatchState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureCore _procedures;
        private ISimulationKernel _simulationKernel;

        public override int StateKey => (int)ProcedureState.Match;

        public ProcedureMatchState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedures) 
            : base(stateMachine)
        {
            _procedures = procedures;
        }

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            _procedures.OnMatchEntered();
            if (!StartGameplay())
            {
                _stateMachine.ChangeState(ProcedureState.Menu);
            }
        }

        public override void Exit()
        {
            StopGameplay();
        }

        /// <summary>
        /// 登记关卡控制、模拟核与本机 pawn。启核失败返回 false，由 Enter 切回菜单。
        /// </summary>
        private bool StartGameplay()
        {
            EventBus.Get<SceneLoadEvent.Completed>().AddListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().AddListener(HandleLevelLoadFailed);
            
            // 注册模拟核
            SessionRole sessionRole = Global.Get<RoomManager>().SessionRole;
            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = sessionRole == SessionRole.Client ? new ClientSimulationKernel() : new HostSimulationKernel();
            systems.RegisterSystem(kernel);
            if (!kernel.StartSession())
            {
                systems.UnregisterSystem(kernel);
                return false;
            }
            _simulationKernel = kernel;
            
            Global.Register<LocalPawnModule>();
            Global.Register<LevelManager>().LoadMatchLevel();
            
            return true;
        }

        private void StopGameplay()
        {
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().RemoveListener(HandleLevelLoadFailed);
            
            // 清掉模拟核
            if (_simulationKernel != null)
            {
                _simulationKernel.StopSession();
                Global.Get<SystemManager>().UnregisterSystem(_simulationKernel);
                _simulationKernel = null;
            }
            
            Global.Unregister<LocalPawnModule>();
            Global.Unregister<LevelManager>();
        }

        private void HandleLevelLoadCompleted(SceneLoadEvent.CompletedData data)
        {
            if (data.SceneName != GlobalConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            Global.Get<LocalPawnModule>().HandleMatchLevelCompleted();
        }

        private void HandleLevelLoadFailed(SceneLoadEvent.FailedData data)
        {
            if (data.SceneName != GlobalConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            _stateMachine.ChangeState(ProcedureState.Menu);
        }
    }
}
