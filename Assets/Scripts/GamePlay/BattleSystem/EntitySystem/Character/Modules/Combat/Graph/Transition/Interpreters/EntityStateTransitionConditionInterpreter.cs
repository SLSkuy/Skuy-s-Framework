using System;

namespace GamePlay.BattleSystem
{
    public sealed class EntityStateTransitionConditionInterpreter : ITransitionConditionInterpreter
    {
        private readonly EntityStateMachineModule _stateMachine;

        public EntityStateTransitionConditionInterpreter(EntityStateMachineModule stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public bool Evaluate(ITransitionCondition condition)
        {
            EntityStateTransitionCondition stateCondition = (EntityStateTransitionCondition)condition;
            return Array.IndexOf(stateCondition.allowedStates, _stateMachine.CurrentState) >= 0;
        }
    }
}
