using UnityEngine;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 生成实体命令。只描述生成数据，不引用预制体或已注册实体。
    /// </summary>
    public struct EntitySpawnCommand : IEntityFactoryCommand
    {
        public EntityType entityTypeId;
        public Vector3 position;
        public Quaternion rotation;

        public EntitySpawnCommand(EntityType typeId)
        {
            entityTypeId = typeId;
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }
        
        public EntitySpawnCommand(EntityType typeId, Vector3 pos, Quaternion rot)
        {
            entityTypeId = typeId;
            position = pos; 
            rotation = rot;
        }
    }
}
