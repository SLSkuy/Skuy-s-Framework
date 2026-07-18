namespace Framework
{
    public enum SubSystemPriority
    {
        SystemManager = int.MinValue,
        NetWorkManager = -114514,
        ResourceManager = -200,
        PoolManager = -150,
        TimerManager = -120,
        DataProxyManager = -100,
        SceneLoader = -50,
        GameStateManager = 0,
        NetSyncManager = 100,
        UIManager = 5000,
    }
}
