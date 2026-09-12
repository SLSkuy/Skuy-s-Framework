using Unity.Entities;
using Unity.Mathematics;

namespace GamePlay.Navigation
{
    /// <summary>
    /// 寻路实体核心，挂载该组件的实体才能被识别为可移动角色
    /// </summary>
    public struct ASAgent : IComponentData
    {
        // ========== Agent移动属性 ==========
        public float MaxSpeed;
        public float TurnSpeed;
        public float StoppingDistance;
        
        // ========== Agent物体属性 ==========
        public float Radius;
        public float Height;

        // ========== ORCA避障参数 ==========
        /// <summary>预测时间</summary>
        public float TimeHorizon;
        public float3 PreferVelocity;
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