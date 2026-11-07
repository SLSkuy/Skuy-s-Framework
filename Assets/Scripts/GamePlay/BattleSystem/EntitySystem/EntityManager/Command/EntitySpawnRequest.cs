using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 生成实体命令。只描述生成数据，不引用预制体或已注册实体。
    /// </summary>
    public struct EntitySpawnRequest
    {
        public EntityType entityType;
        public Vector3 position;
        public Quaternion rotation;

        public EntitySpawnRequest(EntityType type)
        {
            entityType = type;
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        public EntitySpawnRequest(EntityType type, Vector3 pos, Quaternion rot)
        {
            entityType = type;
            position = pos; 
            rotation = rot;
        }
    }
}
