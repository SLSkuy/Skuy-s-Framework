using Unity.Mathematics;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 全局数学工具类，提供各种数学计算函数
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// 转换三维向量到平面上，忽略Y轴
        /// </summary>
        public static float2 ToPlane(float3 vec)
        {
            return new float2(vec.x, vec.z);
        }

        /// <summary>
        /// 将二维向量转换到空间中，Y轴置空
        /// </summary>
        public static float3 ToWorld(float2 vec)
        {
            return new float3(vec.x, 0, vec.y);
        }
        
        /// <summary>
        /// 沿y轴旋转二维向量
        /// </summary>
        public static Vector2 RotateVector2_Y(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// 安全的归一化，如果向量长度为0则返回零向量
        /// </summary>
        public static Vector2 SafeNormalize(Vector2 vector)
        {
            if (vector.sqrMagnitude < Mathf.Epsilon)
                return Vector2.zero;
            return vector.normalized;
        }
        
        /// <summary>
        /// 安全的归一化，如果向量长度为0则返回零向量
        /// </summary>
        public static float2 SafeNormalize(float2 value)
        {
            float lengthSq = math.lengthsq(value);
            return lengthSq > math.EPSILON ? value * math.rsqrt(lengthSq) : float2.zero;
        }

        /// <summary>
        /// 安全的归一化，如果向量长度为0则返回零向量
        /// </summary>
        public static Vector3 SafeNormalize(Vector3 vector)
        {
            if (vector.sqrMagnitude < Mathf.Epsilon)
                return Vector3.zero;
            return vector.normalized;
        }
        
        /// <summary>
        /// 叉乘
        /// </summary>
        public static float Cross(float2 left, float2 right)
        {
            return left.x * right.y - left.y * right.x;
        }
    }
}