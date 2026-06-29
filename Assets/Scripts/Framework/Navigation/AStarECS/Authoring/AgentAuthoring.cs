using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 寻路Agent创建器
    /// </summary>
    public class AgentAuthoring : MonoBehaviour
    {
        [Header("Agent属性设置")] 
        public float maxSpeed;
        public float turnSpeed;
        public float radius;
        public float height;
        
        [Header("Agent寻路设置")]
        public float stoppingDistance;
        [Tooltip("ORCA预测时间，数值越大越早开始避让；<= 0 时使用系统默认值")]
        public float timeHorizon = 2f;
        [Tooltip("是否有初始寻路目的地")]
        public bool hasInitDestination;
        public Vector3 initDestination;
        
        private class AgentBaker : Baker<AgentAuthoring>
        {
            public override void Bake(AgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                ASAgent agent = new ASAgent
                {
                    MaxSpeed = authoring.maxSpeed,
                    TurnSpeed = authoring.turnSpeed,
                    Radius = authoring.radius,
                    Height = authoring.height,
                    StoppingDistance = authoring.stoppingDistance,
                    TimeHorizon = authoring.timeHorizon,
                };

                ASFollower follower = new ASFollower
                {
                    PathAvailable = false,
                    TargetIndex = 0,
                    DestinationReached = false,
                };

                // 添加初始组件，请求相关组件由System动态添加
                AddComponent(entity, agent);
                AddComponent(entity, follower);
                AddBuffer<ASORCALine>(entity);

                // 判断是否有初始目标点，添加请求实例
                if (authoring.hasInitDestination)
                {
                    AddComponent(entity, new ASRequester
                    {
                        Destination = new float3(authoring.initDestination),
                    });
                }
            }
        }
    }
}
