using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Framework
{
    [UpdateAfter(typeof(ObstacleCheckSystem))]
    public partial struct IslandCheckSystem : ISystem
    {
        private bool _hasCheckIsland;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _hasCheckIsland = false;
            
            // 若没有ASGrid组件存在，岛屿检测没有意义
            state.RequireForUpdate<ASGrid>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int currentID = 0;  // 岛屿初始化ID

            foreach (var (grid, entity) in SystemAPI.Query<RefRW<ASGrid>>().WithEntityAccess())
            {
                // 需要刷新岛屿
                if (grid.ValueRO.NeedRefreshIsland)
                {
                    grid.ValueRW.NeedRefreshIsland = false;
                    grid.ValueRW.IslandCreated = true;
                    _hasCheckIsland = false;
                }

                if (!_hasCheckIsland)
                {
                    DynamicBuffer<ASCell> cells = SystemAPI.GetBuffer<ASCell>(entity);

                    int xCount = grid.ValueRO.XCount;
                    int yCount = grid.ValueRO.YCount;
                    
                    NativeArray<bool> visited = new NativeArray<bool>(xCount * yCount, Allocator.Temp);
                    ResetIslandIds(cells);

                    for (int y = 0; y < yCount; y++)
                    {
                        for (int x = 0; x < xCount; x++)
                        {
                            int index = x + y * xCount;

                            if (!visited[index] && cells[index].IsAccessible)
                            {
                                CheckIsland(x, y, xCount, yCount, cells, visited, currentID);
                                currentID++;
                            }
                        }
                    }
                    
                    _hasCheckIsland = true;
                    visited.Dispose();
                }
            }
        }

        /// <summary>
        /// 清理刷新留下的岛屿ID，保证所有不可通行节点固定为 -1
        /// </summary>
        [BurstCompile]
        private void ResetIslandIds(DynamicBuffer<ASCell> cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                cell.IslandID = -1;
                cells[i] = cell;
            }
        }

        /// <summary>
        /// BFS搜索临时节点
        /// </summary>
        private struct CellNode
        {
            public int X;
            public int Y;
        }
        
        /// <summary>
        /// BFS搜索岛屿
        /// </summary>
        [BurstCompile]
        private void CheckIsland(int startX, int startY, int xCount, int yCount,
            DynamicBuffer<ASCell> cells, NativeArray<bool> visited, int currentID)
        {
            NativeQueue<CellNode> q = new NativeQueue<CellNode>(Allocator.Temp);
            q.Enqueue(new CellNode { X = startX, Y = startY });

            while (q.TryDequeue(out var cell))
            {
                int x = cell.X;
                int y = cell.Y;
                
                if(x < 0 || x >= xCount || y < 0 || y >= yCount) continue;
                
                int index = x + y * xCount;
                if(visited[index]) continue;

                // 障碍物不可通行，保留原有-1标识
                if (!cells[index].IsAccessible) continue;
                
                // 标记该节点已访问，设置节点所属的岛屿ID
                visited[index] = true;
                var tmp = cells[index];
                tmp.IslandID = currentID;
                cells[index] = tmp;
                
                q.Enqueue(new CellNode { X = x + 1, Y = y });
                q.Enqueue(new CellNode { X = x - 1, Y = y });
                q.Enqueue(new CellNode { X = x, Y = y + 1 });
                q.Enqueue(new CellNode { X = x, Y = y - 1});
            }
        }
    }
}
