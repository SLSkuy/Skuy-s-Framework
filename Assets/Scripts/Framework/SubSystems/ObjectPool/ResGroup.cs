namespace Framework
{
    /// <summary>
    /// 实例池中的资源分组。同一资源名第一次建池时确定，未传入时为 <see cref="Temp"/>。
    /// UI、Audio 跨场景保留，只按空闲超时卸载。VFX、Prefab、Temp 在切场景时销毁。
    /// </summary>
    public enum ResGroup
    {
        UI = 0,
        VFX = 1,
        Audio = 2,
        Prefab = 3,
        Temp = 4
    }
}
