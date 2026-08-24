using Framework;
using GamePlay.EntitySystem;
using NetSync;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 网络协议对象与游戏状态之间的转换。
    /// </summary>
    public static class ProtoUtils
    {
        public static Vec2 ToProto(Vector2 value)
        {
            return new Vec2 { X = value.x, Y = value.y };
        }

        public static Vec3 ToProto(Vector3 value)
        {
            return new Vec3 { X = value.x, Y = value.y, Z = value.z };
        }

        public static Vector2 ToUnity(Vec2 value)
        {
            return value == null ? Vector2.zero : new Vector2(value.X, value.Y);
        }

        public static Vector3 ToUnity(Vec3 value)
        {
            return value == null ? Vector3.zero : new Vector3(value.X, value.Y, value.Z);
        }

        public static Quat ToProto(Quaternion value)
        {
            value.Normalize();
            return new Quat { X = value.x, Y = value.y, Z = value.z, W = value.w };
        }

        public static Quaternion ToUnity(Quat value)
        {
            if (value == null) return Quaternion.identity;
            Quaternion rotation = new(value.X, value.Y, value.Z, value.W);
            float magnitude = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            return magnitude > Mathf.Epsilon ? new Quaternion(rotation.x / magnitude, rotation.y / magnitude, rotation.z / magnitude, rotation.w / magnitude) : Quaternion.identity;
        }

        public static InputState ToInputState(Player_Input input)
        {
            return new InputState
            {
                MoveInput = ToUnity(input.MoveInput),
                AimInput = ToUnity(input.AimInput),
                IsPrimaryAttackPressed = input.IsPrimaryAttackPressed,
                IsSpecialAttackPressed = input.IsSpecialAttackPressed,
                IsSpecialActionPressed = input.IsSpecialActionPressed,
                IsInteractPressed = input.IsInteractPressed,
                IsSprintPressed = input.IsSprintPressed,
                IsJumpPressed = input.IsJumpPressed,
                IsSwitchModePressed = input.IsSwitchModePressed,
            };
        }

        public static Player_Input ToPlayerInput(uint entityId, uint inputTick, InputState state)
        {
            return new Player_Input
            {
                EntityId = entityId,
                InputTick = inputTick,
                MoveInput = ToProto(state.MoveInput),
                AimInput = ToProto(state.AimInput),
                IsPrimaryAttackPressed = state.IsPrimaryAttackPressed,
                IsSpecialAttackPressed = state.IsSpecialAttackPressed,
                IsSpecialActionPressed = state.IsSpecialActionPressed,
                IsInteractPressed = state.IsInteractPressed,
                IsSprintPressed = state.IsSprintPressed,
                IsJumpPressed = state.IsJumpPressed,
                IsSwitchModePressed = state.IsSwitchModePressed,
            };
        }

        public static Character_Snapshot ToCharacterSnapshotMessage(in EntitySimulationState state, 
            uint entityId, uint ownerClientId, uint snapshotTick, uint lastProcessedInputTick)
        {
            return new Character_Snapshot
            {
                EntityId = entityId,
                SnapshotTick = snapshotTick,
                LastProcessedInputTick = lastProcessedInputTick,
                Position = ToProto(state.position),
                Rotation = ToProto(state.rotation),
                ViewRotation = ToProto(state.viewRotation),
                LinearVelocity = ToProto(state.linearVelocity),
                AngularVelocity = ToProto(state.angularVelocity),
                OwnerClientId = ownerClientId,
                LocomotionState = state.locomotionState,
                IsGrounded = state.isGrounded
            };
        }

        public static EntitySimulationState ToSimulationState(Character_Snapshot snapshot)
        {
            return new EntitySimulationState
            {
                position = ToUnity(snapshot.Position),
                rotation = ToUnity(snapshot.Rotation),
                viewRotation = ToUnity(snapshot.ViewRotation),
                linearVelocity = ToUnity(snapshot.LinearVelocity),
                angularVelocity = ToUnity(snapshot.AngularVelocity),
                locomotionState = snapshot.LocomotionState,
                isGrounded = snapshot.IsGrounded
            };
        }

    }
}
