using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// KD-Tree包围盒定义
    /// </summary>
    public struct KDTreeBound
    {
        /// <summary>
        /// 包围盒的最小角点
        /// </summary>
        public float3 Min;
        
        /// <summary>
        /// 包围盒的最大角点
        /// </summary>
        public float3 Max;
        
        /// <summary>
        /// 包围盒的大小（各轴长度）
        /// </summary>
        public float3 Size => Max - Min;

        /// <summary>
        /// 获取包围盒离point最近的点的位置
        /// 若为0则说明point在包围盒内部，需要进入子树节点查找
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float3 ClosestPoint(float3 point)
        {
            return math.clamp(point, Min, Max);
        }
    }
}