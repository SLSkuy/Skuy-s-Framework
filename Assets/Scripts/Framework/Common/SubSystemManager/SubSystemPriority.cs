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
        NetworkTimeSystem = 40,
        NetSyncManager = 100,
        UIManager = 5000,
        CameraManager = 10000,
    }
}
