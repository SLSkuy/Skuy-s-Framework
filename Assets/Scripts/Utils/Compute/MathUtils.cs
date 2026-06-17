using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 全局数学工具类，提供各种数学计算函数
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// 沿y轴旋转二维向量
        /// </summary>
        /// <param name="v">原始向量</param>
        /// <param name="degrees">旋转角度（度）</param>
        /// <returns>旋转后的向量</returns>
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
        /// <param name="vector">输入向量</param>
        /// <returns>归一化后的向量</returns>
        public static Vector2 SafeNormalize(Vector2 vector)
        {
            if (vector.sqrMagnitude < Mathf.Epsilon)
                return Vector2.zero;
            return vector.normalized;
        }

        /// <summary>
        /// 安全的归一化，如果向量长度为0则返回零向量
        /// </summary>
        /// <param name="vector">输入向量</param>
        /// <returns>归一化后的向量</returns>
        public static Vector3 SafeNormalize(Vector3 vector)
        {
            if (vector.sqrMagnitude < Mathf.Epsilon)
                return Vector3.zero;
            return vector.normalized;
        }
    }
}