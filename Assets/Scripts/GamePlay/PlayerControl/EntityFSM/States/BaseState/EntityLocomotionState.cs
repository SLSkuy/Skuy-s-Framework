namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动状态基类，统一编排每帧的运动调用（旋转 + 位移 + 重力）。
    /// 物理状态由 EntityMotor 集中持有跨状态共享。
    /// _locomotionSpeed 为状态目标速度，由派生状态在 Enter 时设定。
    /// </summary>
    public abstract class EntityLocomotionState : EntityBaseState
    {
        /// <summary>
        /// 当前状态的目标移动速度，由派生状态在 Enter 时设置
        /// </summary>
        protected float _locomotionSpeed;

        protected EntityLocomotionState(EntityContext context) : base(context) { }

        #region 状态控制

        protected override void Tick(float dt)
        {
            Motor.Rotate(Context.LastAimInput, dt);
            Motor.Move(Context.LastMoveInput, _locomotionSpeed, dt);
        }

        /// <summary>
        /// 地面状态通用转换检查：离地→空中、跳跃请求、冲刺请求
        /// 返回 true 表示已切换状态，派生类应停止后续判定
        /// 跳跃请求不切换状态（由物理驱动下一帧进 AIRBORNE），仅调用 Motor.Jump
        /// </summary>
        protected bool CheckGroundTransitions()
        {
            if (!IsGrounded)
            {
                _stateMachine.ChangeState(EntityState.AIRBORNE);
                return true;
            }

            if (JumpRequest)
            {
                Motor.Jump(Config.jumpSpeed, Config.jumpCount);
            }

            if (DashRequest)
            {
                _stateMachine.ChangeState(EntityState.DASH);
                return true;
            }

            return false;
        }

        #endregion
    }
}
