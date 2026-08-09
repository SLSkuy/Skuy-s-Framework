using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体行走状态
    /// </summary>
    public class EntityWalkState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.WALK;
        
        public EntityWalkState(EntityContext context) : base(context) { }
        
        #region 状态控制

        public override void Enter()
        {
            _locomotionSpeed = Config.walkSpeed;
        }

        protected override void CheckStateChange()
        {
            if (Context.LastMoveInput == Vector2.zero)
            {
                _stateMachine.ChangeState(EntityState.IDLE);
            }
        }

        #endregion
    }
}