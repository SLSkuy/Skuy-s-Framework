using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// Agent 移动系统：根据寻路得到的路径点更新 Agent 的位置与朝向。
    /// </summary>
    [UpdateAfter(typeof(PathFindingSystem))]
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

            foreach (var (agentRW, followerRW, transformRW, pathBuffer)
                     in SystemAPI.Query<RefRW<ASAgent>, RefRW<ASFollower>, RefRW<LocalTransform>, DynamicBuffer<ASPathBuffer>>())
            {
                ref ASAgent agent = ref agentRW.ValueRW;
                ref ASFollower follower = ref followerRW.ValueRW;
                ref LocalTransform transform = ref transformRW.ValueRW;

                // 没有可用路径或已到达终点，直接减速并跳过
                if (!follower.PathAvailable || follower.DestinationReached)
                {
                    agent.CurrentVelocity = float3.zero;
                    continue;
                }

                // 检测寻路结果是否可用
                int pathLength = pathBuffer.Length;
                if (pathLength == 0)
                {
                    follower.PathAvailable = false;
                    agent.CurrentVelocity = float3.zero;
                    continue;
                }

                // 边界保护，确保 TargetIndex 在合法范围
                if (follower.TargetIndex >= pathLength)
                {
                    follower.TargetIndex = pathLength - 1;
                }

                float3 currentPos = transform.Position;
                float3 targetPoint = pathBuffer[follower.TargetIndex].Point;

                // 水平方向向量（忽略 y 差异，使 Agent 在当前平面移动）
                float3 toTarget = targetPoint - currentPos;
                toTarget.y = 0f;
                float distanceToPoint = math.length(toTarget);

                // 到达当前路径点：切换到下一个路径点
                if (distanceToPoint <= agent.StoppingDistance)
                {
                    follower.TargetIndex++;
                    if (follower.TargetIndex >= pathLength)
                    {
                        // 已到达最终目的地
                        follower.DestinationReached = true;
                        follower.PathAvailable = false;
                        agent.CurrentVelocity = float3.zero;
                        continue;
                    }

                    // 切换到下一个目标点后重新计算方向
                    targetPoint = pathBuffer[follower.TargetIndex].Point;
                    toTarget = targetPoint - currentPos;
                    toTarget.y = 0f;
                    distanceToPoint = math.length(toTarget);
                }

                // 计算期望速度与朝向
                float3 desiredDir = distanceToPoint > math.EPSILON ? toTarget / distanceToPoint : float3.zero;
                float3 desiredVelocity = desiredDir * agent.MaxSpeed;

                // 简单线性插值：使实际速度平滑过渡到期望速度，避免瞬时速度跳变
                agent.CurrentVelocity = math.lerp(agent.CurrentVelocity, desiredVelocity, math.saturate(agent.TurnSpeed * deltaTime));

                // 更新位置
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
