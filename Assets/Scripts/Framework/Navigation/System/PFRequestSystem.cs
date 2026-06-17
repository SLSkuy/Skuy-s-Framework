using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// 寻路请求系统
    /// </summary>
    public partial struct PFRequestSystem : ISystem
    {
        private ComponentLookup<ASResult> _resultLookup;
        private ComponentLookup<ASOperation> _operationLookup;
        private BufferLookup<ASPathBuffer> _pathBufferLookup;
        private ComponentLookup<ASFollower> _followerLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _resultLookup = state.GetComponentLookup<ASResult>();
            _operationLookup = state.GetComponentLookup<ASOperation>();
            _pathBufferLookup = state.GetBufferLookup<ASPathBuffer>();
            _followerLookup = state.GetComponentLookup<ASFollower>();
            
            state.RequireForUpdate<ASGrid>();
            state.RequireForUpdate<ASAgent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _resultLookup.Update(ref state);
            _operationLookup.Update(ref state);
            _pathBufferLookup.Update(ref state);
            _followerLookup.Update(ref state);
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            bool hasGrid = false;
            ASGrid grid = default;
            DynamicBuffer<ASCell> cells = default;
            LocalTransform gridTransform = default;

            // 初始化网格数据
            foreach (var (gridValue, transform, cellsValue) 
                     in SystemAPI.Query<RefRO<ASGrid>, RefRO<LocalTransform>, DynamicBuffer<ASCell>>())
            {
                hasGrid = true;
                grid = gridValue.ValueRO;
                gridTransform = transform.ValueRO;
                cells = cellsValue;
                break;
            }

            // 等待其他System创建好相应数据
            if (!hasGrid || !cells.IsCreated || !grid.IslandCreated)
            {
                ecb.Dispose();
                return;
            }

            // 处理寻路请求，将请求组件转换为临时寻路操作组件
            foreach (var (requester, transform, entity) in SystemAPI
                         .Query<RefRO<ASRequester>, RefRO<LocalTransform>>()
                         .WithAll<ASAgent>().WithEntityAccess())
            {
                float3 startPos = transform.ValueRO.Position;
                float3 endPos = requester.ValueRO.Destination;

                // 岛屿检测：起点和终点是否在同一可通行区域
                ASCell startCell = GridUtils.WorldPosToCell(startPos, grid, gridTransform, cells);
                ASCell endCell = GridUtils.WorldPosToCell(endPos, grid, gridTransform, cells);

                int startIndex = startCell.X + startCell.Y * grid.XCount;
                int endIndex = endCell.X + endCell.Y * grid.XCount;

                startIndex = math.clamp(startIndex, 0, cells.Length - 1);
                endIndex = math.clamp(endIndex, 0, cells.Length - 1);

                // 不在同一岛屿（或目标不可通行）：标记失败，避免反复请求
                if (cells[startIndex].IslandID != cells[endIndex].IslandID ||
                    cells[startIndex].IslandID < 0 || cells[endIndex].IslandID < 0)
                {
                    ASResult failedResult = new ASResult
                    {
                        FinishedSearch = true,
                        PathFounded = false,
                    };

                    if (_resultLookup.HasComponent(entity))
                        ecb.SetComponent(entity, failedResult);
                    else
                        ecb.AddComponent(entity, failedResult);

                    if (_operationLookup.HasComponent(entity))
                        ecb.RemoveComponent<ASOperation>(entity);

                    if (_pathBufferLookup.HasBuffer(entity))
                        _pathBufferLookup[entity].Clear();

                    if (_followerLookup.HasComponent(entity))
                    {
                        ASFollower follower = _followerLookup[entity];
                        follower.PathAvailable = false;
                        follower.TargetIndex = 0;
                        follower.DestinationReached = false;
                        ecb.SetComponent(entity, follower);
                    }

                    ecb.RemoveComponent<ASRequester>(entity);
                    continue;
                }

                if (_pathBufferLookup.HasBuffer(entity))
                    _pathBufferLookup[entity].Clear();
                else
                    ecb.AddBuffer<ASPathBuffer>(entity);

                ASResult pendingResult = new ASResult
                {
                    FinishedSearch = false,
                    PathFounded = false,
                };

                if (_resultLookup.HasComponent(entity))
                    ecb.SetComponent(entity, pendingResult);
                else
                    ecb.AddComponent(entity, pendingResult);

                ASOperation operation = new ASOperation
                {
                    StartPoint = startPos,
                    TargetPoint = endPos,
                };

                if (_operationLookup.HasComponent(entity))
                    ecb.SetComponent(entity, operation);
                else
                    ecb.AddComponent(entity, operation);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
