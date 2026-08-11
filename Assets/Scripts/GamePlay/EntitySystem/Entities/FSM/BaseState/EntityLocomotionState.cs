namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动状态基类，统一编排每帧的运动调用。
    /// 物理状态由 MovementModule 集中持有并跨状态共享。
    /// </summary>
    public abstract class EntityLocomotionState : EntityBaseState
    {
        protected EntityLocomotionState(EntityContext context) : base(context) { }

        #region 状态控制
        protected override void Tick(float dt)
        {
            Motor.Rotate(Context.LastAimInput, dt);
            Motor.UpdateMeshFacing(Context.LastMoveInput, Context.IsFocus, dt);

            if (Config.rootMotion) return;

            Motor.Move(Context.LastMoveInput, LocomotionSpeed, dt, Context.IsFocus);
        }

        /// <summary>
        /// 地面状态通用转换检查。
        /// </summary>
        protected bool CheckGroundTransitions()
        {
            if (RunToggleRequest)
            {
                Context.IsRunning = !Context.IsRunning;
            }

            if (!IsGrounded)
            {
                _stateMachine.ChangeState(EntityState.AIRBORNE);
                return true;
            }

            if (JumpRequest)
            {
                Motor.Jump(Config.jumpSpeed, Config.jumpCount);
            }

            return false;
        }
        #endregion
    }
}
