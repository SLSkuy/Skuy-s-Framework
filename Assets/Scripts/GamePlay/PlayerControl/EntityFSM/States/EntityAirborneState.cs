using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体空中状态：推进重力与水平移动，落地后回到地面状态。
    /// 空中允许二段跳：直接调用 Motor.Jump，不切换状态（Motor 内部判定 jumpCount 上限）。
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

            // 落地回到地面状态 
            if (IsGrounded)
            {
                _stateMachine.ChangeState(LastMoveInput != Vector2.zero ? EntityState.WALK : EntityState.IDLE);
            }
        }

        #endregion
    }
}
