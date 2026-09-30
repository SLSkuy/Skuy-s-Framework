namespace Framework
{
    /// <summary>
    /// 实例池中的资源分组。同一资源名第一次建池时确定，未传入时为 <see cref="Temp"/>。
    /// UI、Audio 默认跨场景保留。VFX、Prefab、Temp 默认切场景销毁。
    /// 空闲上限、预热和超时见 <see cref="ResGroupPolicy"/>。
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
