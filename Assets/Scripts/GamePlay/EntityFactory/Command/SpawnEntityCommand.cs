using UnityEngine;

namespace GamePlay.EntityFactory
{
    /// <summary>
    /// 生成实体命令。只描述生成数据，不引用预制体或已注册实体。
    /// </summary>
    public struct SpawnEntityCommand
    {
        public EntityType entityTypeId;
        public Vector3 position;
        public Quaternion rotation;

        public SpawnEntityCommand(EntityType entityTypeId)
        {
            this.entityTypeId = entityTypeId;
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }
        
        public SpawnEntityCommand(EntityType entityTypeId, Vector3 position, Quaternion rotation)
        {
            this.entityTypeId = entityTypeId;
            this.position = position;
            this.rotation = rotation;
        }
    }
}
