using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Spawns agents on a ring and drives them directly to the opposite side through ORCA.
    /// </summary>
    public class RingHedgeTestAuthoring : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Custom agent prefab baked with AgentAuthoring.")]
        public GameObject agentPrefab;

        [Header("Ring")]
        [Min(1)] public int agentCount = 64;
        [Min(0.1f)] public float radius = 15f;
        [Min(0f)] public float spawnHeight = 1f;
        public Vector3 center;

        [Header("Debug")]
        public bool logSpawnResult = true;

        private class RingHedgeTestBaker : Baker<RingHedgeTestAuthoring>
        {
            public override void Bake(RingHedgeTestAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
                Entity prefab = authoring.agentPrefab
                    ? GetEntity(authoring.agentPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null;

                AddComponent(entity, new RingHedgeTestConfig
                {
                    AgentPrefab = prefab,
                    AgentCount = math.max(1, authoring.agentCount),
                    Radius = math.max(0.1f, authoring.radius),
                    SpawnHeight = math.max(0f, authoring.spawnHeight),
                    Center = authoring.center,
                    LogSpawnResult = authoring.logSpawnResult,
                });
            }
        }
    }

    public struct RingHedgeTestConfig : IComponentData
    {
        public Entity AgentPrefab;
        public int AgentCount;
        public float Radius;
        public float SpawnHeight;
        public float3 Center;
        public bool LogSpawnResult;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RequestSystem))]
    public partial struct RingHedgeTestSpawnSystem : ISystem
    {
        private bool _hasSpawned;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RingHedgeTestConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_hasSpawned) return;

            RingHedgeTestConfig config = default;
            bool hasConfig = false;
            foreach (var value in SystemAPI.Query<RefRO<RingHedgeTestConfig>>())
            {
                config = value.ValueRO;
                hasConfig = true;
                break;
            }

            if (!hasConfig || config.AgentPrefab == Entity.Null)
            {
                return;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            bool prefabHasRequester = state.EntityManager.HasComponent<ASRequester>(config.AgentPrefab);
            bool prefabHasFollower = state.EntityManager.HasComponent<ASFollower>(config.AgentPrefab);

            for (int i = 0; i < config.AgentCount; i++)
            {
                float angle = math.PI * 2f * i / config.AgentCount;
                float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * config.Radius;
                float3 spawnPosition = config.Center + offset;
                spawnPosition.y = config.Center.y + config.SpawnHeight;

                float3 destination = config.Center - offset;
                destination.y = spawnPosition.y;

                quaternion rotation = quaternion.LookRotationSafe(destination - spawnPosition, math.up());

                Entity agent = ecb.Instantiate(config.AgentPrefab);
                ecb.SetComponent(agent, LocalTransform.FromPositionRotationScale(spawnPosition, rotation, 1f));
                ecb.AddComponent(agent, new RingHedgeTarget
                {
                    Destination = destination,
                });

                if (prefabHasRequester)
                {
                    ecb.RemoveComponent<ASRequester>(agent);
                }

                if (prefabHasFollower)
                {
                    ecb.SetComponent(agent, new ASFollower
                    {
                        PathAvailable = false,
                        TargetIndex = 0,
                        DestinationReached = false,
                    });
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            _hasSpawned = true;

            if (config.LogSpawnResult)
            {
                Debug.Log($"[RingHedgeTest] Spawned {config.AgentCount} agents. Radius: {config.Radius}, Center: {config.Center}");
            }
        }
    }

    public struct RingHedgeTarget : IComponentData
    {
        public float3 Destination;
        public bool Reached;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PreferVelocitySystem))]
    [UpdateBefore(typeof(ORCASystem))]
    public partial struct RingHedgeDirectVelocitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RingHedgeTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (agentRW, targetRW, transform) in
                     SystemAPI.Query<RefRW<ASAgent>, RefRW<RingHedgeTarget>, RefRO<LocalTransform>>())
            {
                ref ASAgent agent = ref agentRW.ValueRW;
                ref RingHedgeTarget target = ref targetRW.ValueRW;

                if (target.Reached)
                {
                    agent.PreferVelocity = float3.zero;
                    continue;
                }

                float3 toTarget = target.Destination - transform.ValueRO.Position;
                toTarget.y = 0f;

                float distance = math.length(toTarget);
                if (distance <= math.max(agent.StoppingDistance, 0.01f))
                {
                    target.Reached = true;
                    agent.PreferVelocity = float3.zero;
                    continue;
                }

                agent.PreferVelocity = toTarget / distance * agent.MaxSpeed;
            }
        }
    }
}
