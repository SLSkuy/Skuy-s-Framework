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
                };

                ASFollower follower = new ASFollower
                {
                    PathAvailable = false,
                    TargetIndex = 0,
                    DestinationReached = false,
                };

                ASRequester requester = new ASRequester
                {
                    Destination = new float3(authoring.initDestination),
                    RequestEntity = entity,
                    RequestGiven = !authoring.hasInitDestination,
                };

                ASResult result = new ASResult
                {
                    PathFounded = false,
                    FinishedSearch = false,
                };

                ASOperation op = new ASOperation
                {
                    StartPoint = float3.zero,
                    TargetPoint = float3.zero,
                };

                AddComponent(entity, agent);
                AddComponent(entity, follower);
                AddComponent(entity, requester);
                AddComponent(entity, result);
                AddComponent(entity, op);
                AddBuffer<ASPathBuffer>(entity);
            }
        }
    }
}
