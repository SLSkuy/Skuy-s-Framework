namespace Framework
{
    /// <summary>
    /// 实例池中的资源分组。同一资源名第一次建池时确定，未传入时为 <see cref="Temp"/>。
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
