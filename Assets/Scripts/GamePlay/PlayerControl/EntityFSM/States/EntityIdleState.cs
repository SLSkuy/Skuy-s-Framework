using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体待机状态
    /// </summary>
    public class EntityIdleState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.IDLE;
        
        public EntityIdleState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}