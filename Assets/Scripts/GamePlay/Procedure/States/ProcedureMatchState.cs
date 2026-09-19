using Core;
using Framework;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：进入时登记关卡控制、模拟核、实体工厂与在场导演；离开时先回收实体再停核。
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
        
        private bool StartGameplay()
        {
            EventBus.Get<SceneLoadEvent.Completed>().AddListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().AddListener(HandleLevelLoadFailed);
            
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
            
            Global.Register<LevelManager>().LoadMatchLevel();
            
            return true;
        }

        private void StopGameplay()
        {
            EventBus.Get<SceneLoadEvent.Completed>().RemoveListener(HandleLevelLoadCompleted);
            EventBus.Get<SceneLoadEvent.Failed>().RemoveListener(HandleLevelLoadFailed);
            
            Global.Unregister<LevelManager>();
            
            if (_simulationKernel != null)
            {
                _simulationKernel.StopSession();
                Global.Get<SystemManager>().UnregisterSystem(_simulationKernel);
                _simulationKernel = null;
            }
        }

        #region 状态周期

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            if (!StartGameplay())
            {
                _stateMachine.ChangeState(ProcedureState.Menu);
            }
        }

        public override void Exit()
        {
            StopGameplay();
        }

        #endregion

        #region 事件回调

        private void HandleLevelLoadCompleted(SceneLoadEvent.CompletedData data)
        {
            if (data.SceneName != GlobalConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            // TODO: 关卡加载完成，委托生成玩家实体
        }

        private void HandleLevelLoadFailed(SceneLoadEvent.FailedData data)
        {
            if (data.SceneName != GlobalConstants.LEVEL_SCENE_NAME)
            {
                return;
            }

            _stateMachine.ChangeState(ProcedureState.Menu);
        }

        #endregion
    }
}
