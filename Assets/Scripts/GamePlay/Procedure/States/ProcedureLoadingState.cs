using Framework;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 加载状态。
    /// 负责执行 PendingTransition 的加载任务，完成后进入目标状态。
    /// </summary>
    public sealed class ProcedureLoadingState : ProcedureStateBase
    {
        private ILoadTask _task;

        public override ProcedureState StateKey => ProcedureState.Loading;

        public ProcedureLoadingState(StateMachine<ProcedureState> stateMachine, ProcedureCore procedure)
            : base(stateMachine, procedure)
        {
        }

        #region 状态周期

        public override void Enter()
        {
            StartTask();
            
            Global.ShowUI("LoadingPanel");
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
        
        private void StartTask()
        {
            StopTask();

            var transition = Procedure.PendingTransition;
            _task = transition.Task;

            _task.Finished += OnTaskFinished;
            _task.Start();
        }

        private void OnTaskFinished()
        {
            var task = _task;
            var transition = Procedure.PendingTransition;

            StopTask();

            if (task.IsFailed)
            {
                Procedure.FailTransition(transition.NextState);
                return;
            }

            _stateMachine.ChangeState(transition.NextState);
        }

        private void StopTask()
        {
            if (_task == null)
                return;

            _task.Finished -= OnTaskFinished;
            _task.Stop();
            _task = null;
        }
    }
}