using System;
using Framework;
using GamePlay.BattleSystem;
using NetSync;

namespace Utils
{
    /// <summary>
    /// 战斗模块相关协议转换定义
    /// </summary>
    public static partial class ProtoUtils
    {
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        
        public static Input_Command ToProto(this in InputCommand input, uint inputTick)
        {
            return new Input_Command
            {
                InputTick = inputTick,
                MoveInput = input.MoveInput.ToProto(),
                AimInput = input.AimInput.ToProto(),
                Flags = (uint)input.FlagsHeld
            };
        }

        public static InputCommand ToCommand(this Input_Command input)
        {
            return new InputCommand
            {
                MoveInput = input.MoveInput.ToUnity(),
                AimInput = input.AimInput.ToUnity(),
                FlagsHeld = (EntityCommandFlags)input.Flags
            };
        }
        
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        
        public static Player_Spawn_Notify ToProto(this EntitySpawnRequest request, uint entityId, Guid playerId, in EntitySnapshot snapshot)
        {
            return new Player_Spawn_Notify
            {
                EntityId = entityId,
                EntityTypeId = (uint)request.entityType,
                PlayerId = playerId.ToString(),
                Position = request.position.ToProto(),
                Rotation = request.rotation.ToProto(),
                Snapshot = snapshot.ToProto(entityId, 0),
            };
        }

        public static EntitySpawnRequest ToCommand(this Player_Spawn_Notify notify)
        {
            return new EntitySpawnRequest
            {
                entityType = (EntityType)notify.EntityTypeId,
                position = notify.Position.ToUnity(),
                rotation = notify.Rotation.ToUnity(),
            };
        }

        public static Entity_Destroy_Notify ToProto(this EntityDestroyRequest request)
        {
            return new Entity_Destroy_Notify
            {
                EntityId = request.entityId,
            };
        }

        public static EntityDestroyRequest ToCommand(this Entity_Destroy_Notify command)
        {
            return new EntityDestroyRequest()
            {
                entityId = command.EntityId,
            };
        }
        
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
    }
}