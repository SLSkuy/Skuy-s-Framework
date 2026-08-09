using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体冲刺状态：冲刺期间方向锁定，持续 dashDuration 后回到地面状态。
    /// 冲刺中不响应跳跃/移动输入（Motor.Move 在 _isDashing 时跳过方向更新，
    /// Motor.Jump 内部拒绝冲刺中跳跃）。
    /// </summary>
    public class EntityDashState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.DASH;

        public EntityDashState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            Motor.StartDash(Context.LastMoveInput, Config.dashSpeed, Config.dashDuration);
        }

        protected override void CheckStateChange()
        {
            // 冲刺期间不响应其他输入，只检测冲刺结束
            if (!IsDashing)
            {
                if (!IsGrounded)
                {
                    _stateMachine.ChangeState(EntityState.AIRBORNE);
                }
                else if (Context.IsSprinting && Context.LastMoveInput != Vector2.zero)
                {
                    _stateMachine.ChangeState(EntityState.SPRINT);
                }
                else if (Context.LastMoveInput != Vector2.zero)
                {
                    _stateMachine.ChangeState(EntityState.WALK);
                }
                else
                {
                    _stateMachine.ChangeState(EntityState.IDLE);
                }
            }
        }

        #endregion
    }
}
