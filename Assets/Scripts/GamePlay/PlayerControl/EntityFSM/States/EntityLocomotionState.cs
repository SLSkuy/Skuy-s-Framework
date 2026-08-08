using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    public abstract class EntityLocomotionState : EntityGroundedState
    {
        protected EntityLocomotionState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}