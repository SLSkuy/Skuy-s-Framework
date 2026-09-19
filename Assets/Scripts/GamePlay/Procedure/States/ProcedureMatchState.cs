using Framework;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：进入时登记关卡控制与模拟核；关卡就绪后再生成本机 pawn。离开时拆除它们。
    /// </summary>
    public sealed class ProcedureMatchState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureCore _procedures;
        private LevelManager _levels;
        private ISimulationKernel _simulationKernel;

        public override int StateKey => (int)ProcedureState.Match;

        public ProcedureMatchState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedures) 
            : base(stateMachine)
        {
            _procedures = procedures;
        }
        
        private bool StartGameplay()
        {
            _levels = Global.Register<LevelManager>();
            _levels.Completed += HandleLevelLoadCompleted;
            _levels.Failed += HandleLevelLoadFailed;

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

            _levels.LoadMatchLevel();
            return true;
        }

        private void StopGameplay()
        {
            if (_levels != null)
            {
                _levels.Completed -= HandleLevelLoadCompleted;
                _levels.Failed -= HandleLevelLoadFailed;
                Global.Unregister<LevelManager>();
                _levels = null;
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
            // TODO: 关卡加载完成，委托生成玩家实体
        }

        private void HandleLevelLoadFailed(string sceneName, string errorMessage)
        {
            _stateMachine.ChangeState(ProcedureState.Menu);
        }

        #endregion
    }
}
