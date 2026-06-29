using Unity.Entities;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 障碍物创建器，桥接ECS
    /// </summary>
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Header("障碍物范围惩罚值")]
        public int obstaclePenalty;
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                Entity obstacleEntity = GetEntity(TransformUsageFlags.Dynamic);

                ASObstacle obstacle = new ASObstacle()
                {
                    PenaltyValue = authoring.obstaclePenalty,
                };
                AddComponent(obstacleEntity, obstacle);
            }
        }
    }
}