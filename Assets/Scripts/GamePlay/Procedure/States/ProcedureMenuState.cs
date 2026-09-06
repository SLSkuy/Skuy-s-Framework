using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 主菜单流程。
    /// </summary>
    public sealed class ProcedureMenuState : EnumStateBase<GameProcedure>
    {
        private readonly ProcedureCore _procedures;

        public override int StateKey => (int)GameProcedure.Menu;

        public ProcedureMenuState(EnumStateMachine<GameProcedure> stateMachine, ProcedureCore procedures)
            : base(stateMachine)
        {
            _procedures = procedures;
        }

        public override void Enter()
        {
            _procedures.TearDownSession();
        }
    }
}
