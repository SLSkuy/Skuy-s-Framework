using Unity.Mathematics;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 全局数学工具类，提供各种数学计算函数
    /// </summary>
    public static class MathUtils
    {
        public static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        
        public static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) 
            && float.IsFinite(value.z) && float.IsFinite(value.w) && value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.000001f;
        
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
            if (vector.sqrMagnitude < Mathf.Epsilon) return Vector2.zero;
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
            if (vector.sqrMagnitude < Mathf.Epsilon) return Vector3.zero;
            return vector.normalized;
        }
        
        public static Quaternion SafeNormalize(Quaternion rotation)
        {
            float length = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + 
                                      rotation.z * rotation.z + rotation.w * rotation.w);
            if (length <= Mathf.Epsilon) return Quaternion.identity;

            float inverse = 1f / length;
            return new Quaternion(rotation.x * inverse, rotation.y * inverse,
                rotation.z * inverse, rotation.w * inverse);
        }
        
        /// <summary>
        /// 叉乘
        /// </summary>
        public static float Cross(float2 left, float2 right)
        {
            return left.x * right.y - left.y * right.x;
        }

        /// <summary>
        /// 标准化角度
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            else if (angle < -180f) angle += 360f;
            return angle;
        }
        
        public static Vector3 CalAngularVelocity(Quaternion previousRotation, Quaternion currentRotation, float deltaTime)
        {
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            return axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized * (angle / deltaTime) : Vector3.zero;
        }
        
        public static float NormalizePitch(float eulerX)
        {
            if (eulerX > 180f) eulerX -= 360f;
            return eulerX;
        }
    }
}
