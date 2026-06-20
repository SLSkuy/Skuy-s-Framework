using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// Agent移动系统，根据计算好的速度朝向移动Agent
    /// </summary>
    [UpdateAfter(typeof(PreferVelocitySystem))]
    public partial struct AgentMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ASGrid>();
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
                
                // TODO: 替换为ORCA的动态避障速度
                // 简单线性插值：使实际速度平滑过渡到期望速度，避免瞬时速度跳变
                agent.CurrentVelocity = math.lerp(agent.CurrentVelocity, agent.PreferVelocity, math.saturate(agent.TurnSpeed * deltaTime));

                // 更新位置
                float3 currentPos = transform.Position;
                float3 moveDelta = agent.CurrentVelocity * deltaTime;
                float3 newPos = currentPos + moveDelta;
                newPos.y = currentPos.y;   // 保持 y 不变，由其他系统负责垂直移动
                transform.Position = newPos;

                // 更新朝向（仅在有明显运动方向时更新，避免抖动导致朝向归零）
                if (math.lengthsq(agent.CurrentVelocity) > 0.0001f)
                {
                    float3 forwardDir = math.normalize(new float3(agent.CurrentVelocity.x, 0f, agent.CurrentVelocity.z));
                    float3 upAxis = new float3(0f, 1f, 0f);

                    // 通过 LookRotation 计算朝向四元数
                    transform.Rotation = quaternion.LookRotation(forwardDir, upAxis);
                }
            }
        }
    }
}
