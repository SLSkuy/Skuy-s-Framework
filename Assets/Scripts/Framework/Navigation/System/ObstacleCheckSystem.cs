using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Framework
{
    public partial struct ObstacleCheckSystem : ISystem
    {
        private bool _hasCheckObstacle;
        private bool _hasCheckPenaltyArea;

        private ComponentLookup<ASObstacle> _obstacles;
        private ComponentLookup<ASPenaltyArea> _penaltyAreas;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _hasCheckObstacle = false;
            _hasCheckPenaltyArea = false;
            
            _obstacles = SystemAPI.GetComponentLookup<ASObstacle>();
            _penaltyAreas = SystemAPI.GetComponentLookup<ASPenaltyArea>();
            
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _obstacles.Update(ref state);
            _penaltyAreas.Update(ref state);
            
            // PhysicsWorldSingleton 是 Unity Physics 提供的单例
            PhysicsWorld physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            foreach (var (grid, transform, entity) in SystemAPI.Query<RefRW<ASGrid>, LocalTransform>().WithEntityAccess())
            {
                // 当前不需要刷新网格，直接返回
                if (!grid.ValueRO.NeedRefreshGrid)
                {
                    grid.ValueRW.NeedRefreshGrid = false;
                    _hasCheckObstacle = false;
                    _hasCheckPenaltyArea = false;
                }
                
                if (!_hasCheckObstacle || !_hasCheckPenaltyArea)
                {
                    // 从网格实体中获取到节点数组组件
                    DynamicBuffer<ASCell> cells = SystemAPI.GetBuffer<ASCell>(entity);
                    RefreshGridPenalty(grid.ValueRO, cells, transform, physicsWorld);

                    // 更新检测标记
                    _hasCheckObstacle = true;
                    _hasCheckPenaltyArea = true;
                    
                    // 通知网格进行岛屿刷新
                    grid.ValueRW.NeedRefreshIsland = true;
                }
            }
        }
        
        /// <summary>
        /// 刷新网格障碍物、惩罚区域检测
        /// </summary>
        /// <param name="gridRO">只读的网格数据</param>
        /// <param name="cells">所有的节点信息</param>
        /// <param name="transform">网格位置</param>
        [BurstCompile]
        private void RefreshGridPenalty(ASGrid gridRO, DynamicBuffer<ASCell> cells, LocalTransform transform, PhysicsWorld physicsWorld)
        {
            for (int y = 0; y < gridRO.YCount; y++)
            {
                for (int x = 0; x < gridRO.XCount; x++)
                {
                    int index = x + y * gridRO.YCount;
                    
                    // 重置节点数据
                    var cell = cells[index];
                    cell.IsAccessible = true;
                    cell.Penalty = 0;
                    cells[index] = cell;
                    
                    // 计算新的节点可通行信息
                    float3 cellBoundBox = new float3(1, 1, 1) * (gridRO.CellSize / 2);
                    float3 worldCoords = GridUtils.GridCoordsToWorld(x, y, gridRO, transform);
                    NativeList<DistanceHit> result = new NativeList<DistanceHit>(Allocator.Temp);

                    // 需要检测的碰撞层级
                    int colliderLayer = 0;
                    if(!_hasCheckObstacle) colliderLayer |= gridRO.ObstacleLayerMask;
                    if (!_hasCheckPenaltyArea) colliderLayer |= gridRO.PenaltyLayerMask;
                    
                    // 碰撞检测
                    bool isAccessible = !physicsWorld.OverlapBox(worldCoords, quaternion.identity,
                        cellBoundBox, ref result, new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = (uint)colliderLayer,
                        GroupIndex = 0
                    });

                    for (int i = 0; i < result.Length; i++)
                    {
                        // 更新节点障碍物阻挡信息
                        if (!_hasCheckObstacle && _obstacles.HasComponent(result[i].Entity))
                        {
                            ASObstacle obstacle = _obstacles[result[i].Entity];
                            var asCell = cells[index];
                            asCell.IsAccessible = isAccessible;
                            asCell.Penalty = obstacle.PenaltyValue;
                            cells[index] = asCell;
                        }

                        // 更新节点惩罚区域叠加数值
                        if (!_hasCheckObstacle && _penaltyAreas.HasComponent(result[i].Entity))
                        {
                            ASPenaltyArea penalty = _penaltyAreas[result[i].Entity];
                            var asCell = cells[index];
                            asCell.Penalty += penalty.PenaltyValue;
                            cells[index] = asCell;
                        }
                    }
                    
                    result.Dispose();
                }
            }
            
            BlurPenalty(gridRO, cells);
        }

        /// <summary>
        /// 箱式滤波惩罚区域
        /// </summary>
        [BurstCompile]
        private void BlurPenalty(ASGrid gridRO, DynamicBuffer<ASCell> cells)
        {
            int width = gridRO.XCount;
            int height = gridRO.YCount;

            int blurSize = gridRO.BlurSize;
            int kernelSize = blurSize * 2 + 1;

            NativeArray<float> horizontalPass = new NativeArray<float>(width * height, Allocator.Temp);
            NativeArray<float> verticalPass = new NativeArray<float>(width * height, Allocator.Temp);

            // =========================
            // 1. 水平方向 blur
            // =========================
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * width;

                // 计算 x = 0 的初始窗口
                float sum = 0;
                for (int x = -blurSize; x <= blurSize; x++)
                {
                    int cx = math.clamp(x, 0, width - 1);
                    int idx = rowStart + cx;
                    sum += cells[idx].Penalty;
                }

                horizontalPass[rowStart] = sum;

                // 滑动窗口
                for (int x = 1; x < width; x++)
                {
                    int removeX = math.clamp(x - blurSize - 1, 0, width - 1);
                    int addX = math.clamp(x + blurSize, 0, width - 1);

                    int removeIdx = rowStart + removeX;
                    int addIdx = rowStart + addX;

                    sum -= cells[removeIdx].Penalty;
                    sum += cells[addIdx].Penalty;

                    horizontalPass[rowStart + x] = sum;
                }
            }

            // =========================
            // 2. 竖直方向 blur
            // =========================
            for (int x = 0; x < width; x++)
            {
                float sum = 0;

                // y = 0 初始窗口
                for (int y = -blurSize; y <= blurSize; y++)
                {
                    int cy = math.clamp(y, 0, height - 1);
                    int idx = cy * width + x;
                    sum += horizontalPass[idx];
                }

                verticalPass[x] = sum;

                // 滑动窗口
                for (int y = 1; y < height; y++)
                {
                    int removeY = math.clamp(y - blurSize - 1, 0, height - 1);
                    int addY = math.clamp(y + blurSize, 0, height - 1);

                    int removeIdx = removeY * width + x;
                    int addIdx = addY * width + x;

                    sum -= horizontalPass[removeIdx];
                    sum += horizontalPass[addIdx];

                    verticalPass[y * width + x] = sum;
                }
            }

            // =========================
            // 3. 写回结果（归一化）
            // =========================
            float invKernel = 1f / (kernelSize * kernelSize);

            for (int i = 0; i < width * height; i++)
            {
                var cell = cells[i];
                cell.Penalty = (verticalPass[i] * invKernel);
                cells[i] = cell;
            }

            horizontalPass.Dispose();
            verticalPass.Dispose();
        }
    }
}