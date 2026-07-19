using Framework;
using GamePlay.EntitySystem;
using NetSync;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 网络同步工具
    /// </summary>
    public static class NetSyncUtils
    {
        public static Vec2 ToProto(Vector2 value)
        {
            return new Vec2
            {
                X = value.x,
                Y = value.y
            };
        }

        public static Vec3 ToProto(Vector3 value)
        {
            return new Vec3
            {
                X = value.x,
                Y = value.y,
                Z = value.z
            };
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
                IsPrimaryAttackPressed = input.PrimaryAttackPressed,
                IsSpecialAttackPressed = input.SpecialAttackPressed,
                IsSpecialActionPressed = input.SpecialActionPressed,
                IsInteractPressed = input.InteractPressed,
                IsSprintPressed = input.SprintPressed,
                IsDashPressed = input.DashPressed
            };
        }

        public static Player_Input ToPlayerInput(uint clientId, uint tick, InputState state)
        {
            return new Player_Input
            {
                ClientId = clientId,
                Tick = tick,
                MoveInput = ToProto(state.MoveInput),
                AimInput = ToProto(state.AimInput),
                PrimaryAttackPressed = state.IsPrimaryAttackPressed,
                SpecialAttackPressed = state.IsSpecialAttackPressed,
                SpecialActionPressed = state.IsSpecialActionPressed,
                InteractPressed = state.IsInteractPressed,
                SprintPressed = state.IsSprintPressed,
                DashPressed = state.IsDashPressed
            };
        }

        public static Player_Snapshot ToPlayerSnapshot(uint clientId, NetPlayerSnapshot snapshot)
        {
            return new Player_Snapshot
            {
                ClientId = clientId,
                Tick = snapshot.Tick,
                Position = ToProto(snapshot.Position),
                Rotation = ToProto(snapshot.Rotation)
            };
        }

        public static NetPlayerSnapshot ToPlayerSnapshot(Player_Snapshot snapshot)
        {
            return new NetPlayerSnapshot
            {
                Tick = snapshot.Tick,
                Position = ToUnity(snapshot.Position),
                Rotation = ToUnity(snapshot.Rotation)
            };
        }
    }
}
