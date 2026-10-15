using Framework;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 主菜单界面状态
    /// </summary>
    public sealed class ProcedureMenuState : ProcedureStateBase
    {
        public override ProcedureState StateKey => ProcedureState.Menu;

        public ProcedureMenuState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine, procedure)
        {
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
