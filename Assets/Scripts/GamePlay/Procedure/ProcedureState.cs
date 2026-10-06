namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程：菜单、开局准备、对局。换场景是状态切换上的任务，不是一个状态。没有大厅。
    /// </summary>
    public enum ProcedureState
    {
        Menu = 0,
        Preparing = 1,
        Loading = 2,
        Match = 3,
    }
}
