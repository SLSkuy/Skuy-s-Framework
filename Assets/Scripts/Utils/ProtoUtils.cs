using Framework;
using GamePlay.EntityFactory;
using GamePlay.EntitySystem;
using UnityEngine;
using NetSync;

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

        public static Player_Input ToPlayerInput(uint inputTick, InputState state)
        {
            return new Player_Input
            {
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

        public static Player_Snapshot ToPlayerSnapshot(uint entityId, uint lastProcessedInputTick,
            in EntitySnapshot state)
        {
            MovementSnapshot movement = state.movement;
            return new Player_Snapshot
            {
                EntityId = entityId,
                LocomotionState = (uint)state.state.entityState,
                Position = ToProto(movement.rootPosition),
                Rotation = ToProto(movement.meshRotation),
                ViewRotation = ToProto(state.view.viewRotation),
                LinearVelocity = ToProto(movement.rootLinearVelocity),
                AngularVelocity = ToProto(movement.meshAngularVelocity),
                LastProcessedInputTick = lastProcessedInputTick,
            };
        }

        public static Entity_Spawn_Notify ToEntitySpawnNotify(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            return new Entity_Spawn_Notify
            {
                EntityId = entityId,
                EntityTypeId = (uint)entityTypeId,
                PlayerId = playerId,
                Position = ToProto(position),
                Rotation = ToProto(rotation),
            };
        }

        public static EntitySnapshot ToRollbackState(Player_Snapshot snapshot)
        {
            Quaternion viewRotation = ToUnity(snapshot.ViewRotation);
            Vector3 euler = viewRotation.eulerAngles;
            return new EntitySnapshot
            {
                state = new StateSnapshot
                {
                    entityState = (EntityState)snapshot.LocomotionState
                },
                movement = new MovementSnapshot
                {
                    rootPosition = ToUnity(snapshot.Position),
                    meshRotation = ToUnity(snapshot.Rotation),
                    rootLinearVelocity = ToUnity(snapshot.LinearVelocity),
                    meshAngularVelocity = ToUnity(snapshot.AngularVelocity),
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
