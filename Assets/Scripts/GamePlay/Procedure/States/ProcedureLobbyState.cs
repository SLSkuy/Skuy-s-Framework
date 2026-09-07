using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 大厅流程：拥有战局会话寿命。
    /// </summary>
    public sealed class ProcedureLobbyState : EnumStateBase<GameProcedure>
    {
        private readonly ProcedureCore _procedures;

        public override int StateKey => (int)GameProcedure.Lobby;

        public ProcedureLobbyState(EnumStateMachine<GameProcedure> stateMachine, ProcedureCore procedures)
            : base(stateMachine)
        {
            _procedures = procedures;
        }
    }
}
