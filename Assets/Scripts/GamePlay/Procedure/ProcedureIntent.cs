namespace GamePlay.Procedure
{
    /// <summary>
    /// 界面投递给流程的意图。由当前流程状态决定响应与否，调用方不判断流程状态。
    /// </summary>
    public enum ProcedureIntent
    {
        LocalPlay = 0,
        HostMultiplay = 1,
        JoinRemote = 2,
        BackToMenu = 3
    }
}
