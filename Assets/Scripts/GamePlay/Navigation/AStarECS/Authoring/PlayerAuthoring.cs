using Unity.Entities;
using UnityEngine;

namespace GamePlay.Navigation
{
    /// <summary>
    /// 玩家创建器，使用混合结构，桥接ECS
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        private class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity player = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(player, new ASPlayer());
            }
        }
    }
}