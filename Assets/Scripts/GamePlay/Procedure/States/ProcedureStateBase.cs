using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程状态基类。界面意图投递到当前状态，转移条件由状态自己持有。
    /// </summary>
    public abstract class ProcedureStateBase : StateBase<ProcedureState>
    {
        protected readonly ProcedureCore Procedure;

        protected ProcedureStateBase(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine)
        {
            Procedure = procedure;
        }

        /// <summary>
        /// 处理界面意图。当前状态不认识的意图不响应。
        /// </summary>
        public virtual void ProcessIntent(ProcedureIntent intent)
        {

        }
    }
}
