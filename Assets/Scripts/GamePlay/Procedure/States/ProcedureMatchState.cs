using Framework;
using GamePlay.GamePlayManager;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程状态，粘合其他的所有游戏功能模块。进入时关卡已经就绪。
    /// </summary>
    public sealed class ProcedureMatchState : ProcedureStateBase
    {
        public ProcedureMatchState(StateMachine<ProcedureState> stateMachine, ProcedureModule procedure)
            : base(stateMachine, procedure) { }
        
        /// <summary>
        /// TODO: 委托GameManager进行游戏逻辑调度，便于后续ProcedureCore重构开发
        /// </summary>
        private readonly GameManager _gameManager = new();
        
        #region 属性
        public override ProcedureState StateKey => ProcedureState.Match;
        #endregion
        
        #region 状态周期

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.Locked;
            _gameManager.InitGamePlay();
        }

        public override void Exit()
        {
            Cursor.lockState = CursorLockMode.None;
            _gameManager.DestroyGamePlay();
        }
        
        #endregion
    }
}
