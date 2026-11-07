using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 实体待机状态
    /// </summary>
    public class EntityIdleState : EntityLocomotionState
    {
        public override EntityState StateKey => EntityState.IDLE;

        public EntityIdleState(EntityContext context) : base(context) { }

        #region 状态控制

        public override void Enter()
        {
            Context.DesiredSpeed = 0;
        }

        protected override void CheckStateChange()
        {
            if (CheckGroundTransitions()) return;
            
            if (LastMoveInput == Vector2.zero) return;
            
            // 有移动输入时按档位进入对应地面状态
            _stateMachine.ChangeState(IsRunning ? EntityState.RUN : EntityState.WALK);
        }

        #endregion
    }
}
