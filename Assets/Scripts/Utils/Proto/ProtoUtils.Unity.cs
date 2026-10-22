using NetSync;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Unity相关数据结构网络协议转换定义
    /// </summary>
    public static partial class ProtoUtils
    {
        public static Vec2 ToProto(this Vector2 value)
        {
            return new Vec2 { X = value.x, Y = value.y };
        }
        
        public static Vector2 ToUnity(this Vec2 value)
        {
            return value == null ? Vector2.zero : new Vector2(value.X, value.Y);
        }
        
        public static Vec3 ToProto(this Vector3 value)
        {
            return new Vec3 { X = value.x, Y = value.y, Z = value.z };
        }
        
        public static Vector3 ToUnity(this Vec3 value)
        {
            return value == null ? Vector3.zero : new Vector3(value.X, value.Y, value.Z);
        }
        
        public static Quat ToProto(this Quaternion value)
        {
            value.Normalize();
            return new Quat { X = value.x, Y = value.y, Z = value.z, W = value.w };
        }

        public static Quaternion ToUnity(this Quat value)
        {
            if (value == null) return Quaternion.identity;
            Quaternion rotation = new(value.X, value.Y, value.Z, value.W);
            float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            return magnitude > Mathf.Epsilon ? new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude) : Quaternion.identity;
        }
    }
}