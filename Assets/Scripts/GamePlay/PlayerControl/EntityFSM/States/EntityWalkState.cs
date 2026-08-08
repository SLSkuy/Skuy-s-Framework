using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体移动状态
    /// </summary>
    public class EntityWalkState : EntityLocomotionState
    {
        public override uint StateKey => EntityState.WALK;
        
        public EntityWalkState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}