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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ASGrid>();
            state.RequireForUpdate<ASAgent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            bool hasGrid = false;
            ASPathFindingConfig config = default;
            DynamicBuffer<ASCell> cells = default;
            ASGrid grid = default;
            LocalTransform gridTransform = default;

            // 初始化网格数据
            foreach (var (gridValue, configValue, transform, cellsValue) in SystemAPI.Query<RefRO<ASGrid>, RefRO<ASPathFindingConfig>,
                     RefRO<LocalTransform>, DynamicBuffer<ASCell>>())
            {
                hasGrid = true;
                grid = gridValue.ValueRO;
                gridTransform = transform.ValueRO;
                config = configValue.ValueRO;
                cells = cellsValue;
                break;
            }

            // 等待其他System创建好相应数据
            if (!hasGrid || !cells.IsCreated || !grid.IslandCreated)
            {
                ecb.Dispose();
                return;
            }

            // 处理巡礼请求，将寻路请求转换为寻路操作
            int processed = 0;
            int maxPerFrame = config.MaxComputePerFrame > 0 ? config.MaxComputePerFrame : 10;
            foreach (var (requester, transform, result, entity) in SystemAPI
                         .Query<RefRW<ASRequester>, RefRO<LocalTransform>, RefRW<ASResult>>().WithEntityAccess())
            {
                if (processed >= maxPerFrame) break;
                if (requester.ValueRO.RequestGiven) continue; // 已处理过的跳过

                float3 startPos = transform.ValueRO.Position;
                float3 endPos = requester.ValueRO.Destination;

                // 岛屿检测：起点和终点是否在同一可通行区域
                ASCell startCell = GridUtils.WorldPosToCell(startPos, grid, gridTransform, cells);
                ASCell endCell = GridUtils.WorldPosToCell(endPos, grid, gridTransform, cells);

                int startIndex = startCell.X + startCell.Y * grid.XCount;
                int endIndex = endCell.X + endCell.Y * grid.YCount;

                startIndex = math.clamp(startIndex, 0, cells.Length - 1);
                endIndex = math.clamp(endIndex, 0, cells.Length - 1);

                // 不在同一岛屿（或目标不可通行）：标记失败，避免反复请求
                if (cells[startIndex].IslandID != cells[endIndex].IslandID ||
                    cells[startIndex].IslandID < 0 || cells[endIndex].IslandID < 0)
                {
                    requester.ValueRW.RequestGiven = true;
                    result.ValueRW.FinishedSearch = true;
                    result.ValueRW.PathFounded = false;
                    continue;
                }

                // 为该 Agent 写入 ASPathFindingOperation（由 PathFindingSystem 消费）
                ecb.SetComponent(entity, new ASOperation
                {
                    StartPoint = startPos,
                    TargetPoint = endPos,
                });

                // 重置寻路结果状态（表示正在寻路）
                result.ValueRW.FinishedSearch = false;
                result.ValueRW.PathFounded = false;

                // 标记请求已派发
                requester.ValueRW.RequestGiven = true;

                processed++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}