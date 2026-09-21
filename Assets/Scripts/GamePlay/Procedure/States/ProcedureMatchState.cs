using Framework;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程状态，粘合其他的所有游戏功能模块
    /// </summary>
    public sealed class ProcedureMatchState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureCore _procedures;
        private LevelManager _levelMgr;
        private ISimulationKernel _simulationKernel;

        public override int StateKey => (int)ProcedureState.Match;

        public ProcedureMatchState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedures) 
            : base(stateMachine)
        {
            _procedures = procedures;
        }
        
        private bool StartGameplay()
        {
            _levelMgr = Global.Register<LevelManager>();
            _levelMgr.Completed += HandleLevelLoadCompleted;
            _levelMgr.Failed += HandleLevelLoadFailed;

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
            
            _levelMgr.LoadMatchLevel();
            return true;
        }

        private void StopGameplay()
        {
            if (_levelMgr != null)
            {
                _levelMgr.Completed -= HandleLevelLoadCompleted;
                _levelMgr.Failed -= HandleLevelLoadFailed;
                Global.Unregister<LevelManager>();
                _levelMgr = null;
            }

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

        private void HandleLevelLoadCompleted(string sceneName)
        {
            // TODO: 生成玩家实体位置
        }

        private void HandleLevelLoadFailed(string sceneName, string errorMessage)
        {
            _stateMachine.ChangeState(ProcedureState.Menu);
        }

        #endregion
    }
}
