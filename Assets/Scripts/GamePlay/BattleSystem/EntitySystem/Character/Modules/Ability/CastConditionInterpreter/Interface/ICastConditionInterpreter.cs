namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 条件解释器，判断执行条件是否符合
    /// </summary>
    public interface ICastConditionInterpreter
    {
        /// <summary>
        /// 解释判断条件是否符合
        /// </summary>
        bool Evaluate(ICastCondition castCondition);
    }
}