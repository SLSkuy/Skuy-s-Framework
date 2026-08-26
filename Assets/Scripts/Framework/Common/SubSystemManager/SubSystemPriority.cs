namespace Framework
{
    /// <summary>
    /// Framework内子系统优先级定义
    /// 外部子系统直接填写优先级数字
    /// 不在此处定义
    /// </summary>
    public enum SubSystemPriority : int
    {
        SystemManager = int.MinValue,
        NetWorkManager = -114514,
        ResourceManager = -200,
        PoolManager = -150,
        TimerManager = -120,
        DataProxyManager = -100,
        SceneLoader = -50,
        GameStateManager = 0,
        UIManager = 5000,
        CameraManager = 10000,
    }
}
