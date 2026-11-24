namespace GamePlay.BattleSystem
{
    public interface ITransitionConditionInterpreter
    {
        /// <summary>
        /// 判断转换条件是否符合
        /// </summary>
        bool Evaluate(ITransitionCondition transitionCondition);
    }
}