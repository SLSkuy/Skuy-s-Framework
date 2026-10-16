using Framework;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程状态，粘合其他的所有游戏功能模块。进入时关卡已经就绪。
    /// </summary>
    public sealed class ProcedureMatchState : ProcedureStateBase
    {
        public override ProcedureState StateKey => ProcedureState.Match;

        public ProcedureMatchState(StateMachine<ProcedureState> stateMachine, ProcedureModule procedure)
            : base(stateMachine, procedure)
        {
        }
        
        #region 状态周期

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        #endregion
    }
}
