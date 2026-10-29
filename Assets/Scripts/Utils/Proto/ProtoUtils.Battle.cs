using System;
using Framework;
using GamePlay.EntitySpawn;
using GamePlay.EntitySystem;
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
                IsPrimaryAttackPressed = input.IsPrimaryAttackPressed,
                IsSpecialAttackPressed = input.IsSpecialAttackPressed,
                IsSpecialActionPressed = input.IsSpecialActionPressed,
                IsInteractPressed = input.IsInteractPressed,
                IsSprintPressed = input.IsSprintPressed,
                IsJumpPressed = input.IsJumpPressed,
                IsSwitchModePressed = input.IsSwitchModePressed,
            };
        }

        public static InputCommand ToCommand(this Input_Command input)
        {
            return new InputCommand
            {
                MoveInput = input.MoveInput.ToUnity(),
                AimInput = input.AimInput.ToUnity(),
                IsPrimaryAttackPressed = input.IsPrimaryAttackPressed,
                IsSpecialAttackPressed = input.IsSpecialAttackPressed,
                IsSpecialActionPressed = input.IsSpecialActionPressed,
                IsInteractPressed = input.IsInteractPressed,
                IsSprintPressed = input.IsSprintPressed,
                IsJumpPressed = input.IsJumpPressed,
                IsSwitchModePressed = input.IsSwitchModePressed,
            };
        }
        
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        // ========== 玩家控制命令 ==========
        
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        
        public static Player_Spawn_Notify ToProto(this EntitySpawnCommand command, uint entityId, Guid playerId, in EntitySnapshot snapshot)
        {
            return new Player_Spawn_Notify
            {
                EntityId = entityId,
                EntityTypeId = (uint)command.entityType,
                PlayerId = playerId.ToString(),
                Position = command.position.ToProto(),
                Rotation = command.rotation.ToProto(),
                Snapshot = snapshot.ToProto(entityId, 0),
            };
        }

        public static EntitySpawnCommand ToCommand(this Player_Spawn_Notify notify)
        {
            return new EntitySpawnCommand
            {
                entityType = (EntityType)notify.EntityTypeId,
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
        
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
        // ========== 战局事件 ==========
    }
}
