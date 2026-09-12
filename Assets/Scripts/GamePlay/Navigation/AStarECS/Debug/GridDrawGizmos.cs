using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace GamePlay.Navigation
{
    /// <summary>
    /// Grid Debug工具，绘制当前的网格信息
    /// </summary>
    public class GridDrawGizmos : MonoBehaviour
    {
        [Header("Debug颜色")]
        [SerializeField] private Color gridColor;
        [SerializeField] private Color gridWireColor;
        [SerializeField] private Color obstaclesColor;

        [Header("绘制选项")]
        [SerializeField] private bool showGrid;
        [SerializeField] private bool showPenalties;
        [SerializeField] private bool showIslands;
        
        [Header("网格全局参数")]
        [SerializeField] private int maxPenalty;
        
        /// <summary>
        /// EntityManager 是访问 ECS Entity 和 Component 的入口。
        /// 可以通过它查询 Entity、读取 Component，以及获取 DynamicBuffer。
        /// </summary>
        private EntityManager _manager;

        private void OnDrawGizmos()
        {
            // 默认 World 创建后才能取得 EntityManager。
            // OnDrawGizmos 可能在非 Play 状态执行，因此先确认 World 存在。
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            if (_manager == default)
            {
                _manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            }

            if (!showGrid)
            {
                return;
            }

            // EntityQuery 表示筛选条件。
            // 这里只查询同时拥有网格配置、Transform 和节点 Buffer 的 Entity，
            // 避免后面读取某个不存在的 Component。
            EntityQuery query = _manager.CreateEntityQuery(
                ComponentType.ReadOnly<ASGrid>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<ASCell>());

            // Temp 表示这份临时 NativeArray 只在当前短时操作中使用。
            // 使用结束后必须 Dispose。
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            query.Dispose();

            foreach (Entity entity in entities)
            {
                // 普通 Component：一个 Entity 上读取一份 ASGrid 和 LocalTransform。
                ASGrid grid = _manager.GetComponentData<ASGrid>(entity);
                LocalTransform transform = _manager.GetComponentData<LocalTransform>(entity);

                // Buffer Component：一个 Entity 上读取一整个可变长度节点数组。
                // cells[index] 返回 ASGridCellBufferElement，再通过 .Cell 取得节点数据。
                DynamicBuffer<ASCell> cells = _manager.GetBuffer<ASCell>(entity, true);

                // 读取岛屿 ID Buffer（buffer 可能已添加但尚未 Resize，需检查长度）
                bool hasIslandBuffer = _manager.HasBuffer<ASCell>(entity);
                DynamicBuffer<ASCell> islands = default;
                int islandBufferLength = 0;
                if (hasIslandBuffer)
                {
                    islands = _manager.GetBuffer<ASCell>(entity, true);
                    islandBufferLength = islands.Length;
                    hasIslandBuffer = islandBufferLength > 0;
                }

                Vector3 gridCenter = transform.Position;
                Vector3 startCorner = gridCenter
                    - Vector3.right * (grid.XCount * grid.CellSize * 0.5f)
                    - Vector3.forward * (grid.YCount * grid.CellSize * 0.5f);

                for (int y = 0; y < grid.YCount; y++)
                {
                    for (int x = 0; x < grid.XCount; x++)
                    {
                        // 必须与 Baker 写入 Buffer 时使用相同的一维索引公式。
                        int index = x + y * grid.XCount;
                        ASCell cell = cells[index];

                        // startCorner 是整个网格的左下角。
                        // 加上半格尺寸后，得到当前格子的中心位置。
                        Vector3 cellCenter = startCorner
                            + Vector3.right * ((x + 0.5f) * grid.CellSize)
                            + Vector3.forward * ((y + 0.5f) * grid.CellSize);
                        Vector3 cellSize = new Vector3(grid.CellSize, 0.1f, grid.CellSize);

                        // 绘制梯度惩罚区域
                        Gizmos.color = cell.IsAccessible ? gridColor : Color.red;

                        // 岛屿模式：根据 IslandID 使用不同颜色
                        if (showIslands && hasIslandBuffer && cell.IsAccessible && index < islandBufferLength)
                        {
                            int islandID = islands[index].IslandID;
                            if (islandID >= 0)
                            {
                                Gizmos.color = GetIslandColor(islandID);
                            }
                        }

                        if (showPenalties && Application.isPlaying && cell.Penalty > 0)
                        {
                            float pen = cell.Penalty / maxPenalty;
                            Gizmos.color = new Color(pen, pen, pen);
                        }
                        Gizmos.DrawCube(cellCenter, cellSize);

                        Gizmos.color = gridWireColor;
                        Gizmos.DrawWireCube(cellCenter, cellSize);
                    }
                }
            }
        }

        /// <summary>
        /// 根据岛屿 ID 生成确定的随机颜色，用于区分不同岛屿。
        /// </summary>
        private static Color GetIslandColor(int islandID)
        {
            // 使用简单的哈希生成伪随机色调
            uint hash = (uint)islandID * 2654435761u;
            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
    }
}
