namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 一次施法的状态，完成和中断后不能继续调度
    /// </summary>
    public enum SkillState
    {
        Created,
        Running,
        Completed,
        Interrupted
    }
}