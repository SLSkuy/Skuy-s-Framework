using Unity.Mathematics;

namespace Framework
{
    public static class ORCAUtils
    {
        public static float2 ToPlane(float3 vec)
        {
            return new float2(vec.x, vec.z);
        }

        public static float3 ToWorld(float2 vec)
        {
            return new float3(vec.x, 0, vec.y);
        }

        public static float Cross(float2 left, float2 right)
        {
            return left.x * right.y - left.y * right.x;
        }

        public static float2 SafeNormalize(float2 value)
        {
            float lengthSq = math.lengthsq(value);
            return lengthSq > math.EPSILON ? value * math.rsqrt(lengthSq) : float2.zero;
        }
    }
}