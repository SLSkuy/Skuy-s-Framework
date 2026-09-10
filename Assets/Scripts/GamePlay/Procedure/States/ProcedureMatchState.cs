using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程：由玩法粘合点加载关卡并启动模拟核。
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
