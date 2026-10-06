using System;

namespace Framework
{
    /// <summary>
    /// 状态切换请求，等待<see cref="Task"/>任务完成之后，才会进入<see cref="NextState"/>
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    public struct ProcedureTransition<TEnum> where TEnum : Enum
    {
        public TEnum NextState { get; }
        public ILoadTask Task { get; }

        public ProcedureTransition(TEnum nextState, ILoadTask task)
        {
            NextState = nextState;
            Task = task;
        }
    }
}