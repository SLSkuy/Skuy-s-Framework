using Unity.Entities;
using UnityEngine;

namespace GamePlay.Navigation
{
    /// <summary>
    /// 网格创建器，桥接ECS
    /// </summary>
    public class GridAuthoring : MonoBehaviour
    {
        [Header("网格属性")]
        [Tooltip("网格在世界空间中的总尺寸，X 对应世界 X 轴，Y 对应世界 Z 轴。")] public Vector2Int gridSize;
        [Tooltip("单个正方形节点的边长。")] public float cellSize;
        [Tooltip("障碍物模糊范围")] public int blurSize = 2;
        
        [Header("寻路属性")]
        [Tooltip("每帧最大处理寻路请求次数")] public int maxComputePerFrame = 10;
        [Tooltip("当目标节点不可达时，向外拓展的最大节点数")] public int targetInaccessibleSearchTolerance = 10;

        [Header("障碍物层级")]
        public LayerMask obstacleLayerMask;
        public LayerMask penaltyLayerMask;

        private class GridBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                Entity gridEntity = GetEntity(TransformUsageFlags.Dynamic);

                // 初始化网格组件
                ASGrid grid = new ASGrid()
                {
                    XCount = authoring.gridSize.x,
                    YCount = authoring.gridSize.y,
                    CellSize = authoring.cellSize,
                    BlurSize = authoring.blurSize,
                    TargetInaccessibleSearchTolerance = authoring.targetInaccessibleSearchTolerance,
                    ObstacleLayerMask = authoring.obstacleLayerMask,
                    PenaltyLayerMask = authoring.penaltyLayerMask,
                    IslandCreated = false
                };
                AddComponent(gridEntity, grid);
                
                // 初始化网格节点
                DynamicBuffer<ASCell> cells = AddBuffer<ASCell>(gridEntity);
                for (int y = 0; y < grid.YCount; y++)
                {
                    for (int x = 0; x < grid.XCount; x++)
                    {
                        cells.Add(new ASCell()
                        {
                            X = x,
                            Y = y,
                            IsAccessible = true,
                        });
                    }
                }
                
                // 初始化寻路配置
                ASPathFindingConfig config = new ASPathFindingConfig()
                {
                    MaxComputePerFrame = authoring.maxComputePerFrame
                };
                AddComponent(gridEntity, config);
            }
        }
    }
}