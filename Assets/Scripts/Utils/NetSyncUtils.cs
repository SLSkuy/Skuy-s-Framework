using Framework;
using GamePlay.EntitySystem;
using NetSync;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 网络协议对象与游戏状态之间的转换。
    /// </summary>
    public static class NetSyncUtils
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

        /// <summary>
        /// 当前协议仍复用 Transform_Snapshot，但运行时只读写 Position / Velocity / MovementState，不处理 Rotation。
        /// </summary>
        public static Position_Snapshot ToPositionSnapshotMessage(in NetPositionSnapshot snapshot)
        {
            return new Position_Snapshot
            {
                EntityId = snapshot.EntityId,
                SnapshotTick = snapshot.SnapshotTick,
                LastProcessedInputTick = snapshot.LastProcessedInputTick,
                Position = ToProto(snapshot.Position),
                Velocity = ToProto(snapshot.Velocity),
            };
        }

        /// <summary>
        /// 当前协议仍复用 Transform_Snapshot，但运行时只读写 Position / Velocity / MovementState，不处理 Rotation。
        /// </summary>
        public static NetPositionSnapshot ToNetPositionSnapshot(Position_Snapshot snapshot)
        {
            return new NetPositionSnapshot
            {
                EntityId = snapshot.EntityId,
                SnapshotTick = snapshot.SnapshotTick,
                LastProcessedInputTick = snapshot.LastProcessedInputTick,
                Position = ToUnity(snapshot.Position),
                Velocity = ToUnity(snapshot.Velocity),
            };
        }

        /// <summary>
        /// 计算玩家状态的位置差距
        /// </summary>
        public static float SnapshotPosDistance(NetPositionSnapshot authority, NetPositionSnapshot predict)
        {
            return Vector3.Distance(authority.Position, predict.Position);
        }
    }
}
