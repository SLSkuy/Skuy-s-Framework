using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 定时翻滚：沿身体朝向持续移动，允许输入转向，期间忽略跳跃与再次翻滚请求。
    /// </summary>
    public class EntityRollState : EntityLocomotionState
    {
        public override EntityState StateKey => EntityState.ROLL;

        public EntityRollState(EntityContext context) : base(context) { }

        public override void Enter()
        {
            Context.DesiredSpeed = Config.rollSpeed;
            Context.RollRemainingTime = Config.rollDuration;
        }

        public override void Exit()
        {
            Context.RollRemainingTime = 0f;
        }

        protected override void Tick(float dt)
        {
            float moveTime = Mathf.Min(dt, Context.RollRemainingTime);
            Movement.Move(Vector2.up, Config.rollSpeed * (moveTime / dt), View.Yaw, dt);
            Context.RollRemainingTime = Mathf.Max(0f, Context.RollRemainingTime - dt);
        }

        protected override void CheckStateChange()
        {
            if (RunToggleRequest) Context.IsRunning = !Context.IsRunning;
            if (Context.RollRemainingTime > 0f) return;

            if (!IsGrounded) _stateMachine.ChangeState(EntityState.AIRBORNE);
            else if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else if (IsSprinting) _stateMachine.ChangeState(EntityState.SPRINT);
            else _stateMachine.ChangeState(IsRunning ? EntityState.RUN : EntityState.WALK);
        }
    }
}