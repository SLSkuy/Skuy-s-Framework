using Unity.Entities;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 惩罚区域创建器，桥接ECS
    /// </summary>
    public class PenaltyAreaAuthoring : MonoBehaviour
    {
        [Header("惩罚区域惩罚值")]
        public int penaltyAreaValue;
        
        private class PenaltyAreaBaker : Baker<PenaltyAreaAuthoring>
        {
            public override void Bake(PenaltyAreaAuthoring authoring)
            {
                Entity penaltyAreaEntity = GetEntity(TransformUsageFlags.Dynamic);

                ASPenaltyArea penaltyAreaArea = new ASPenaltyArea()
                {
                    PenaltyValue = authoring.penaltyAreaValue
                };
                AddComponent(penaltyAreaEntity, penaltyAreaArea);
            }
        }
    }
}