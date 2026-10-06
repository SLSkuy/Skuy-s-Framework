namespace GamePlay.Procedure
{
    /// <summary>
    /// 一次带任务的流程切换。任务完成之后才进入 <see cref="NextState"/>。同时只有一个。
    /// </summary>
    public struct ProcedureTransition
    {
        public ProcedureState NextState { get; }
        public ILoadTask Task { get; }

        public ProcedureTransition(ProcedureState nextState, ILoadTask task)
        {
            NextState = nextState;
            Task = task;
        }
    }
}