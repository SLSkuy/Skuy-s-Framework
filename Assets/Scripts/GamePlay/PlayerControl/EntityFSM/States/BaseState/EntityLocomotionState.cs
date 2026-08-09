using UnityEngine;

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
            // 奔跑模式切换（toggle）：在所有运动状态中统一处理，保证地面/空中均能响应。
            // 切换 IsRunning 后由各状态 CheckStateChange 决定是否流转 WALK/RUN。
            if (RunToggleRequest)
            {
                Context.IsRunning = !Context.IsRunning;
            }

            Motor.Rotate(Context.LastAimInput, dt);
            Motor.Move(Context.LastMoveInput, _locomotionSpeed, dt);
        }

        /// <summary>
        /// 地面状态通用转换检查：离地→空中、跳跃请求。
        /// 返回 true 表示已切换状态，派生类应停止后续判定。
        /// 跳跃请求不切换状态（由物理驱动下一帧进 AIRBORNE），仅调用 Motor.Jump。
        /// 注：奔跑 toggle 已在 Tick 中统一处理，此处无需重复。
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

            // Dash 已暂时屏蔽，保留字段供未来恢复
            // if (DashRequest)
            // {
            //     _stateMachine.ChangeState(EntityState.DASH);
            //     return true;
            // }

            return false;
        }

        /// <summary>
        /// 根据当前输入选择应处的地面状态：
        /// 无移动 → IDLE；Sprint 键按住 → SPRINT（最高优先级）；否则按奔跑 toggle 选 RUN / WALK。
        /// ChangeState 内部会忽略同状态自切换，可放心调用。
        /// </summary>
        protected uint GetGroundMoveState()
        {
            if (LastMoveInput == Vector2.zero) return EntityState.IDLE;
            if (IsSprinting) return EntityState.SPRINT;
            return IsRunning ? EntityState.RUN : EntityState.WALK;
        }

        #endregion
    }
}
