using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体空中状态：推进重力与水平移动，落地后回到地面状态。
    /// 空中允许二段跳：直接调用 Motor.Jump，不切换状态（Motor 内部判定 jumpCount 上限）。
    /// 落地默认回 walk/run（不含 sprint），静止回 IDLE；即使按住 Sprint 也先落地再由地面状态自切 SPRINT。
    /// </summary>
    public class EntityAirborneState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.AIRBORNE;

        public EntityAirborneState(EntityContext context) : base(context) { }

        #region 状态控制

        protected override void CheckStateChange()
        {
            // 空中二段跳（不切状态，Motor 内部判定 jumpCount 上限）
            if (JumpRequest)
            {
                Motor.Jump(Config.jumpSpeed, Config.jumpCount);
            }

            // 落地：默认回 walk/run，静止回 IDLE（不含 sprint）
            if (IsGrounded)
            {
                if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
                else _stateMachine.ChangeState(IsRunning ? EntityState.RUN : EntityState.WALK);
            }
        }

        #endregion
    }
}
