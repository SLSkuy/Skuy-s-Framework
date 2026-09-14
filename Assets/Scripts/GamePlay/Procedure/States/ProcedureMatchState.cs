using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：进入时登记玩法粘合点，由粘合点自己切关并启核。
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
    }
}
