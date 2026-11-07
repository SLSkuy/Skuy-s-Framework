using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class EntityActionState : EntityBaseState
    {
        public EntityActionState(EntityContext context) : base(context) { }

        #region 属性
        public override EntityState StateKey => EntityState.ACTION;
        #endregion
        
        protected override void Tick(float deltaTime)
        {
            // 动作状态统一推进能力，结束后根据当前输入和接地状态返回移动状态。
            Ability.Tick(deltaTime);
        }

        protected override void CheckStateChange()
        {
            if (Ability.IsCasting) return;
            
            if (!IsGrounded)
            {
                _stateMachine.ChangeState(EntityState.AIRBORNE);
                return;
            }
            
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else _stateMachine.ChangeState(IsRunning ? EntityState.RUN : EntityState.WALK);
        }
    }
}
