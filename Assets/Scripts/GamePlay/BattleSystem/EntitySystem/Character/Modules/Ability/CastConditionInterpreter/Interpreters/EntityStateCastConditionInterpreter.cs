namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 根据角色状态机的当前状态解释条件
    /// </summary>
    public sealed class EntityStateCastConditionInterpreter : ICastConditionInterpreter
    {
        private readonly EntityStateMachineModule _stateMachine;

        public EntityStateCastConditionInterpreter(EntityContext context)
        {
            _stateMachine = context.Get<EntityStateMachineModule>();
        }

        public bool Evaluate(ICastCondition castCondition)
        {
            EntityStateCastCondition stateCastCondition = (EntityStateCastCondition)castCondition;
            foreach (EntityState state in stateCastCondition.exclusiveStates)
            {
                if (_stateMachine.CurrentState == state) return false;
            }

            return true;
        }
    }
}