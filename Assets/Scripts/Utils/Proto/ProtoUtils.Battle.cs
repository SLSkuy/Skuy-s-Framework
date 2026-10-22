using System;
using GamePlay.EntitySpawn;
using NetSync;

namespace Utils
{
    /// <summary>
    /// 战斗模块相关协议转换定义
    /// </summary>
    public static partial class ProtoUtils
    {
        public static Entity_Spawn_Notify ToProto(this EntitySpawnCommand command, uint entityId, Guid playerId)
        {
            return new Entity_Spawn_Notify
            {
                EntityId = entityId,
                EntityTypeId = (uint)command.entityTypeId,
                PlayerId = playerId.ToString(),
                Position = command.position.ToProto(),
                Rotation = command.rotation.ToProto(),
            };
        }

        public static EntitySpawnCommand ToCommand(this Entity_Spawn_Notify notify)
        {
            return new EntitySpawnCommand
            {
                entityTypeId = (EntityType)notify.EntityTypeId,
                position = notify.Position.ToUnity(),
                rotation = notify.Rotation.ToUnity(),
            };
        }

        public static Entity_Destroy_Notify ToProto(this EntityDestroyCommand command)
        {
            return new Entity_Destroy_Notify
            {
                EntityId = command.entityId,
            };
        }

        public static EntityDestroyCommand ToCommand(this Entity_Destroy_Notify command)
        {
            return new EntityDestroyCommand()
            {
                entityId = command.EntityId,
            };
        }
    }
}
