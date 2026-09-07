using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：切关卡，就绪后开战。
    /// </summary>
    public sealed class ProcedureMatchState : EnumStateBase<GameProcedure>
    {
        private readonly ProcedureCore _procedures;

        public override int StateKey => (int)GameProcedure.Match;

        public ProcedureMatchState(EnumStateMachine<GameProcedure> stateMachine, ProcedureCore procedures)
            : base(stateMachine)
        {
            _procedures = procedures;
        }

        public override void Enter()
        {
            _procedures.OnMatchEntered();
        }

        public override void Exit()
        {
            _procedures.OnMatchExited();
        }
    }
}
