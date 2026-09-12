using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace GamePlay.Navigation
{
    /// <summary>
    /// Agent移动系统，根据ORCASystem计算好的CurrentVelocity移动Agent
    /// 该系统不再重新计算避障速度，只负责位置积分和朝向更新
    /// </summary>
    [UpdateAfter(typeof(ORCASystem))]
    public partial struct AgentMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ASGrid>();
            state.RequireForUpdate<ASAgent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (agentRW, transformRW) in
                     SystemAPI.Query<RefRW<ASAgent>, RefRW<LocalTransform>>())
            {
                ref ASAgent agent = ref agentRW.ValueRW;
                ref LocalTransform transform = ref transformRW.ValueRW;

                // ORCASystem已经把CurrentVelocity限制到安全速度范围内，这里只做位移积分
                float3 currentPos = transform.Position;
                float3 moveDelta = agent.CurrentVelocity * deltaTime;
                float3 newPos = currentPos + moveDelta;
                newPos.y = currentPos.y;   // 保持y不变，垂直方向由其它系统或场景摆放负责
                transform.Position = newPos;

                // 仅在速度足够明显时更新朝向，避免接近静止时因为浮点误差抖动
                if (math.lengthsq(agent.CurrentVelocity) > 0.0001f)
                {
                    float3 forwardDir = math.normalize(new float3(agent.CurrentVelocity.x, 0f, agent.CurrentVelocity.z));
                    float3 upAxis = new float3(0f, 1f, 0f);
                    quaternion targetRotation = quaternion.LookRotation(forwardDir, upAxis);

                    float t = math.saturate(agent.TurnSpeed * deltaTime);
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation, t);
                }
            }
        }
    }
}
