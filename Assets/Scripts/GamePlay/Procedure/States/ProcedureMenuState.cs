using Framework;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 主菜单流程。
    /// </summary>
    public sealed class ProcedureMenuState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureCore _procedures;

        public override int StateKey => (int)ProcedureState.Menu;

        public ProcedureMenuState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedures)
            : base(stateMachine)
        {
            _procedures = procedures;
        }

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.None;
            
            _procedures.TearDownSession();
        }
    }
}
