using Framework;

namespace GamePlay.Procedure
{
    public class ProcedureLoadingState : ProcedureStateBase
    {
        public override ProcedureState StateKey => ProcedureState.Loading;  
        
        public ProcedureLoadingState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure) 
            : base(stateMachine, procedure)
        {
        }
    }
}