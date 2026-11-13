namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 根据角色状态机的当前状态解释条件
    /// </summary>
    public sealed class EntityStateConditionInterpreter : IConditionInterpreter
    {
        private readonly EntityStateMachineModule _stateMachine;

        public EntityStateConditionInterpreter(EntityContext context)
        {
            _stateMachine = context.Get<EntityStateMachineModule>();
        }

        public bool Evaluate(ICondition condition)
        {
            EntityStateCondition stateCondition = (EntityStateCondition)condition;
            foreach (EntityState state in stateCondition.exclusiveStates)
            {
                if (_stateMachine.CurrentState == state) return false;
            }

            return true;
        }
    }
}