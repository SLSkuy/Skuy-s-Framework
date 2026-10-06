namespace GamePlay.Procedure
{
    /// <summary>
    /// 玩法流程：菜单、开局准备、加载、对局。加载状态跑切换任务，完成后再进入目标状态。没有大厅。
    /// </summary>
    public enum ProcedureState
    {
        Menu = 0,
        Preparing = 1,
        Loading = 2,
        Match = 3,
    }
}
