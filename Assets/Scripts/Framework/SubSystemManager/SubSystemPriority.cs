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
        LevelManager = 50,
        SpawnManager = 60,
        CameraManager = 1000,
        UIManager = 5000,
    }
}
