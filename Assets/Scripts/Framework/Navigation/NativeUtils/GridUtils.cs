using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Framework
{
    /// <summary>
    /// 网格坐标转换工具
    /// </summary>
    public static class GridUtils
    {
        /// <summary>
        /// 网格坐标转换到世界坐标
        /// </summary>
        public static float3 GridCoordsToWorld(int x, int y, ASGrid grid, LocalTransform transform)
        {
            float3 startCorner = GetGridStartCorner(grid, transform);
            
            return startCorner 
                   + math.right() * (x * grid.CellSize + grid.CellSize / 2f)
                   + math.forward() * (y * grid.CellSize + grid.CellSize / 2f);
        }

        /// <summary>
        /// 获取网格左下角坐标
        /// </summary>
        public static float3 GetGridStartCorner(ASGrid grid, LocalTransform transform)
        {
            float3 gridCenter = transform.Position;
            float3 startCorner = gridCenter
                                  - math.right() * (grid.XCount * grid.CellSize * 0.5f)
                                  - math.forward() * (grid.YCount * grid.CellSize * 0.5f);

            return startCorner;
        }
        
        public static ASCell WorldPosToCell(float3 worldPos, ASGrid grid,
            LocalTransform gridTransform, DynamicBuffer<ASCell> cells)
        {
            float gridWidth = grid.XCount * grid.CellSize;
            float gridHeight = grid.YCount * grid.CellSize;

            float localX = worldPos.x - gridTransform.Position.x + gridWidth * 0.5f;
            float localY = worldPos.z - gridTransform.Position.z + gridHeight * 0.5f;

            int x = math.clamp((int)math.floor(localX / grid.CellSize),
                0, grid.XCount - 1);

            int y = math.clamp((int)math.floor(localY / grid.CellSize),
                0, grid.YCount - 1);

            int index = x + y * grid.XCount;

            return cells[index];
        }

        /// <summary>
        /// 获取节点的F代价
        /// </summary>
        public static int CellFCost(ASCell cell)
        {
            return cell.HCost + cell.GCost;
        }
        
        /// <summary>
        /// 获取两节点之间的距离
        /// </summary>
        public static int CellDistance(ASCell cell1, ASCell cell2)
        {
            int xDis = math.abs(cell1.X - cell2.X);
            int yDis = math.abs(cell1.Y - cell2.Y);

            if (xDis > yDis)
                return 14 * yDis + 10 * (xDis - yDis);

            return 14 * xDis + 10 * (yDis - xDis);
        }
        
        /// <summary>
        /// 获取当前节点邻居节点的方位
        /// </summary>
        public static NativeArray<int2> CellNeighbours(Allocator allocator)
        {
            NativeArray<int2> offsetIndexes = new NativeArray<int2>(8, allocator);

            // 4 个正交方向（代价 10）
            offsetIndexes[0] = new int2(1, 0);   // 右
            offsetIndexes[4] = new int2(-1, 0);  // 左
            offsetIndexes[1] = new int2(0, 1);   // 上
            offsetIndexes[5] = new int2(0, -1);  // 下

            // 4 个斜向方向（代价 14）
            offsetIndexes[2] = new int2(1, 1);   // 右上
            offsetIndexes[3] = new int2(-1, -1); // 左下
            offsetIndexes[6] = new int2(-1, 1);  // 左上
            offsetIndexes[7] = new int2(1, -1);  // 右下

            return offsetIndexes;
        }
    }
}