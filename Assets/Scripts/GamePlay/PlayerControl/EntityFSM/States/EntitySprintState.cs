using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体疾跑状态
    /// </summary>
    public class EntitySprintState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.SPRINT;

        public EntitySprintState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            _locomotionSpeed = Config.sprintSpeed;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;

            // 释放疾跑或停止移动时切回
            if (!IsSprinting || LastMoveInput == Vector2.zero)
            {
                _stateMachine.ChangeState(LastMoveInput != Vector2.zero ? EntityState.WALK : EntityState.IDLE);
            }
        }

        #endregion
    }
}
