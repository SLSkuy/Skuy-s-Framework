using Unity.Entities;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// 寻路实体核心，挂载该组件的实体才能被识别为可移动角色
    /// </summary>
    public struct ASAgent : IComponentData
    {
        public float MaxSpeed;
        public float TurnSpeed;
        public float Radius;
        public float Height;
        public float StoppingDistance;

        public float3 CurrentVelocity;
    }

    /// <summary>
    /// 预制体缓存，由于ECS无法直接引用GameObject，因此通过Component间接引用预制体对应的Entity
    /// </summary>
    public struct ASAgentPrefab : IComponentData
    {
        public Entity Prefab;
    }
}