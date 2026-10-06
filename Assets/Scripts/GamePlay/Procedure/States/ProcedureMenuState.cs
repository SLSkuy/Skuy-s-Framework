using Framework;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 主菜单流程。进入时菜单场景已经就绪，这里只负责呈现菜单；
    /// 上一场对局的拆除由 ProcedureCore 在进入菜单的那条边上完成。
    /// </summary>
    public sealed class ProcedureMenuState : ProcedureStateBase
    {
        public override ProcedureState StateKey => ProcedureState.Menu;

        public ProcedureMenuState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine, procedure)
        {
        }

        /// <summary>
        /// 菜单接受三种开局意图：按意图打开对局，再进入准备。
        /// </summary>
        public override void ProcessIntent(ProcedureIntent intent)
        {
            if (intent == ProcedureIntent.BackToMenu) return;

            Procedure.OpenSession(intent);
            Procedure.RequestState(ProcedureState.Preparing);
        }

        #region 状态周期

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.None;

            Global.ShowUI("MainScenePanel");
        }

        #endregion
    }
}
