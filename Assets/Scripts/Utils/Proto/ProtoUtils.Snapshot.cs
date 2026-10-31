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
                LocomotionState = (uint)state.entityState,
                RollRemainingTime = state.rollRemainingTime,
                Position = movement.rootPosition.ToProto(),
                Rotation = movement.meshRotation.ToProto(),
                ViewRotation = snapshot.view.viewRotation.ToProto(),
                // LinearVelocity = movement.rootLinearVelocity.ToProto(),
                // AngularVelocity = movement.meshAngularVelocity.ToProto(),
                LastProcessedInputTick = lastProcessedInputTick,
                VerticalVelocity = movement.verticalVelocity,
                JumpCount = movement.jumpCount,
                IsRunning = state.isRunning,
                IsSprinting = state.isSprinting,
                IsGrounded = movement.isGrounded,
                LocomotionSpeed = state.locomotionSpeed,
                LastMoveDirection = movement.lastMoveDirection.ToProto(),
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
                    entityState = (EntityState)snapshot.LocomotionState,
                    rollRemainingTime = snapshot.RollRemainingTime,
                    locomotionSpeed = snapshot.LocomotionSpeed,
                    isRunning = snapshot.IsRunning,
                    isSprinting = snapshot.IsSprinting,
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
                }
            };
        }
    }
}