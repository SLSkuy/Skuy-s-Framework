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
        private readonly ProcedureCore _procedure;
        private ISimulationKernel _simulationKernel;
        
        #region 游戏逻辑调度器
        private LevelManager _levelMgr;
        private RoomManager _room;
        #endregion

        public ProcedureMatchState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedure) : base(stateMachine)
        {
            _procedure = procedure;
        }

        public override int StateKey => (int)ProcedureState.Match;
        
        private bool StartGameplay()
        {
            _room = Global.Get<RoomManager>();
            _room.PlayerJoined += OnPlayerJoined;
            _room.PlayerRemoved += OnPlayerRemoved;
            
            // 注册模拟核，确定如何进行游戏逻辑Tick
            SessionRole sessionRole = _room.SessionRole;
            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = sessionRole == SessionRole.Client ? new ClientSimulationKernel() : new HostSimulationKernel();
            systems.RegisterSystem(kernel);
            if (!kernel.StartSession())
            {
                systems.UnregisterSystem(kernel);
                return false;
            }
            _simulationKernel = kernel;
            
            _levelMgr = Global.Register<LevelManager>();
            _levelMgr.Completed += OnLevelLoadCompleted;
            _levelMgr.Failed += OnLevelLoadFailed;
            _levelMgr.LoadMatchLevel();
            
            return true;
        }

        private void StopGameplay()
        {
            if (_room != null)
            {
                _room.PlayerJoined -= OnPlayerJoined;
                _room.PlayerRemoved -= OnPlayerRemoved;
                _room = null;
            }
            
            if (_levelMgr != null)
            {
                _levelMgr.Completed -= OnLevelLoadCompleted;
                _levelMgr.Failed -= OnLevelLoadFailed;
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

        private void OnLevelLoadCompleted(string sceneName)
        {
            // TODO: 关卡加载完成回调
        }

        private void OnLevelLoadFailed(string sceneName, string errorMessage)
        {
            _stateMachine.ChangeState(ProcedureState.Menu);
        }

        private void OnPlayerJoined(uint playerId)
        {
            // TODO: 新玩家加入处理
        }

        private void OnPlayerRemoved(uint playerId)
        {
            // TODO: 玩家退出处理
        }

        #endregion
    }
}
