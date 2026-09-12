using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace GamePlay.Navigation
{
    /// <summary>
    /// 计算Agent的期望速度，为ORCA提供数据基础
    /// </summary>
    [UpdateAfter(typeof(PathFindingSystem))]
    public partial struct PreferVelocitySystem : ISystem
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
            foreach (var (agentRW, followerRW, transformRW, pathBuffers) in 
                     SystemAPI.Query<RefRW<ASAgent>, RefRW<ASFollower>, RefRW<LocalTransform>, DynamicBuffer<ASPathBuffer>>())
            {
                ref ASAgent agent = ref agentRW.ValueRW;
                ref ASFollower follower = ref followerRW.ValueRW;
                ref LocalTransform transform = ref transformRW.ValueRW;

                // 没有可用路径或已到达终点，直接减速并跳过
                if (!follower.PathAvailable || follower.DestinationReached)
                {
                    agent.PreferVelocity = float3.zero;
                    continue;
                }
                
                // 检查寻路结果是否可用
                int pathLength = pathBuffers.Length;
                if (pathLength == 0)
                {
                    agent.PreferVelocity = float3.zero;
                    follower.PathAvailable = false;
                    continue;
                }

                // 边界保护，确保TargetIndex在合法范围
                if (follower.TargetIndex >= pathLength)
                {
                    follower.TargetIndex = pathLength - 1;
                }

                float3 currentPos = transform.Position;
                float3 targetPos = pathBuffers[follower.TargetIndex].Point;
                float3 toTarget = targetPos - currentPos;
                toTarget.y = 0;
                float distance = math.length(toTarget);

                // 到达路径节点，切换到下一个路径点
                if (distance <= agent.StoppingDistance)
                {
                    follower.TargetIndex++;
                    if (follower.TargetIndex >= pathLength)
                    {
                        // 已到达目标节点
                        follower.DestinationReached = true;
                        follower.PathAvailable = false;
                        agent.PreferVelocity = float3.zero;
                        continue;
                    }
                    
                    // 更新下一个节点数据
                    targetPos = pathBuffers[follower.TargetIndex].Point;
                    toTarget = targetPos - currentPos;
                    toTarget.y = 0;
                    distance = math.length(toTarget);
                }

                // 更新Agent的期望速度
                float3 preferDir = distance > math.EPSILON ? toTarget / distance : float3.zero;
                float3 preferVelocity = preferDir * agent.MaxSpeed;
                agent.PreferVelocity = preferVelocity;
            }
        }
    }
}