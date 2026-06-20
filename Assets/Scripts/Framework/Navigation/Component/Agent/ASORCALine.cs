using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// ORCA约束线，速度必须处于这条线的一侧，满足所有的ORCA约束线实现动态避障
    /// </summary>
    public struct ASORCALine
    {
        public float3 Point;
        public float3 Direction;
    }
}