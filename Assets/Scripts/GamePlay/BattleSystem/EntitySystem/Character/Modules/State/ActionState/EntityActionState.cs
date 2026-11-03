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
            // Action状态由能力组件内部处理，不再由状态机处理能力
            Ability.Tick(deltaTime);
        }

        protected override void CheckStateChange()
        {
            if (Ability.IsCasting) return;
            
            if (!IsGrounded)
            {
                _stateMachine.ChangeState(EntityState.AIRBORNE);
            }
            
            if (LastMoveInput == Vector2.zero) _stateMachine.ChangeState(EntityState.IDLE);
            else _stateMachine.ChangeState(IsRunning ? EntityState.RUN : EntityState.WALK);
        }
    }
}