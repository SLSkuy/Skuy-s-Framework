using GamePlay.BattleSystem;
using UnityEngine;
using NetSync;

namespace Utils
{
    /// <summary>
    /// 网络协议对象与游戏状态之间的转换。
    /// </summary>
    public static partial class ProtoUtils
    {
        public static Player_Snapshot ToProto(this in EntitySnapshot snapshot, uint entityId, uint lastProcessedInputTick)
        {
            MovementSnapshot movement = snapshot.movement;
            StateSnapshot state = snapshot.state;
            return new Player_Snapshot
            {
                EntityId = entityId,
                LastProcessedInputTick = lastProcessedInputTick,
                
                // StateSnapshot
                EntityState = (uint)state.entityState,
                LocomotionSpeed = state.locomotionSpeed,
                IsRunning = state.isRunning,
                
                //  MovementSnapshot
                Position = movement.rootPosition.ToProto(),
                Rotation = movement.meshRotation.ToProto(),
                LastMoveDirection = movement.lastMoveDirection.ToProto(),
                VerticalVelocity = movement.verticalVelocity,
                JumpCount = movement.jumpCount,
                IsGrounded = movement.isGrounded,
                
                // ViewSnapshot
                ViewRotation = snapshot.view.viewRotation.ToProto(),
                
                // AbilitySnapshot
                SkillId = snapshot.ability.skillId,
                RemainTime = snapshot.ability.remainTime,
                
                // LinearVelocity = movement.rootLinearVelocity.ToProto(),
                // AngularVelocity = movement.meshAngularVelocity.ToProto(),
            };
        }

        public static EntitySnapshot ToSnapshot(this Player_Snapshot snapshot)
        {
            Quaternion viewRotation = snapshot.ViewRotation.ToUnity();
            Vector3 euler = viewRotation.eulerAngles;
            return new EntitySnapshot
            {
                state = new StateSnapshot
                {
                    entityState = (EntityState)snapshot.EntityState,
                    locomotionSpeed = snapshot.LocomotionSpeed,
                    isRunning = snapshot.IsRunning,
                },
                movement = new MovementSnapshot
                {
                    rootPosition = snapshot.Position.ToUnity(),
                    meshRotation = snapshot.Rotation.ToUnity(),
                    // rootLinearVelocity = snapshot.LinearVelocity.ToUnity(),
                    // meshAngularVelocity = snapshot.AngularVelocity.ToUnity(),
                    verticalVelocity = snapshot.VerticalVelocity,
                    jumpCount = snapshot.JumpCount,
                    isGrounded = snapshot.IsGrounded,
                    lastMoveDirection = snapshot.LastMoveDirection.ToUnity(),
                },
                view = new ViewSnapshot
                {
                    viewRotation = viewRotation,
                    yaw = euler.y,
                    pitch = MathUtils.NormalizePitch(euler.x)
                },
                ability = new AbilitySnapshot
                {
                    skillId = snapshot.SkillId,
                    remainTime = snapshot.RemainTime,
                }
            };
        }
    }
}