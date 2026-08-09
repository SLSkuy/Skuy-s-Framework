namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体运动状态基类，统一编排每帧的运动调用（旋转 + 位移 + 重力）。
    /// 物理状态由 EntityMotor 集中持有跨状态共享。
    /// </summary>
    public abstract class EntityLocomotionState : EntityBaseState
    {
        protected EntityLocomotionState(EntityContext context) : base(context) { }

        #region 状态控制

        protected override void Tick(float dt)
        {
            Motor.Rotate(Context.LastAimInput, dt);
            Motor.Move(Context.LastMoveInput, LocomotionSpeed, dt);
        }

        /// <summary>
        /// 地面状态通用转换检查：离地→空中、跳跃请求。
        /// 返回 true 表示已切换状态，派生类应停止后续判定。
        /// 跳跃请求不切换状态（由物理驱动下一帧进 AIRBORNE），仅调用 Motor.Jump。
        /// </summary>
        protected bool CheckGroundTransitions()
        {
            // 奔跑模式切换（toggle）
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

            // Dash 已暂时屏蔽，保留字段供未来恢复
            // if (DashRequest)
            // {
            //     _stateMachine.ChangeState(EntityState.DASH);
            //     return true;
            // }

            return false;
        }

        #endregion
    }
}
