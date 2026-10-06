using Framework;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 加载闸门。进入后跑一段切换任务，成功才进入目标状态。
    /// </summary>
    public sealed class ProcedureLoadingState : ProcedureStateBase
    {
        private ProcedureTransition<ProcedureState> _transition;
        private ILoadTask _task;

        public override ProcedureState StateKey => ProcedureState.Loading;

        public ProcedureLoadingState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine, procedure)
        {
        }
        
        #region 状态周期

        public override void Enter()
        {
            _transition = Procedure.PendingTransition;
            _task = _transition.Task;
            _task.Finished += OnTaskFinished;
            _task.Start();
        }

        public override void Exit()
        {
            StopTask();
        }

        protected override void Tick(float deltaTime)
        {
            _task?.Update(deltaTime);
        }

        #endregion

        private void OnTaskFinished()
        {
            ILoadTask task = _task;
            ProcedureState next = _transition.NextState;
            StopTask();

            if (task.IsFailed)
            {
                _stateMachine.NotifyTransitionFailed(_transition);
                return;
            }

            _stateMachine.ChangeState(next);
        }

        private void StopTask()
        {
            if (_task == null) return;

            _task.Finished -= OnTaskFinished;
            _task.Stop();
            _task = null;
        }
    }
}
