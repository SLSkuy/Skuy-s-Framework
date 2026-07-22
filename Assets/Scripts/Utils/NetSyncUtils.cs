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
            };
        }

        public static Player_Snapshot ToPlayerSnapshot(in NetPlayerSnapshot snapshot)
        {
            return new Player_Snapshot
            {
                EntityId = snapshot.EntityId,
                SnapshotTick = snapshot.SnapshotTick,
                LastProcessedInputTick = snapshot.LastProcessedInputTick,
                Position = ToProto(snapshot.Position),
                Rotation = ToProto(snapshot.Rotation)
            };
        }

        public static NetPlayerSnapshot ToPlayerSnapshot(Player_Snapshot snapshot)
        {
            return new NetPlayerSnapshot
            {
                EntityId = snapshot.EntityId,
                SnapshotTick = snapshot.SnapshotTick,
                LastProcessedInputTick = snapshot.LastProcessedInputTick,
                Position = ToUnity(snapshot.Position),
                Rotation = ToUnity(snapshot.Rotation)
            };
        }

        /// <summary>
        /// 计算玩家状态的位置差距
        /// </summary>
        public static float SnapshotPosDistance(NetPlayerSnapshot authority, NetPlayerSnapshot predict)
        {
            return Vector3.Distance(authority.Position, predict.Position);
        }
    }
}
