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
    public static partial class ProtoUtils
    {
        public static InputState ToInputState(Player_Input input)
        {
            return ToInputState(input.MoveInput, input.AimInput, input.IsPrimaryAttackPressed,
                input.IsSpecialAttackPressed, input.IsSpecialActionPressed, input.IsInteractPressed,
                input.IsSprintPressed, input.IsJumpPressed, input.IsSwitchModePressed);
        }

        public static InputState ToInputState(Input_Sample input)
        {
            return ToInputState(input.MoveInput, input.AimInput, input.IsPrimaryAttackPressed,
                input.IsSpecialAttackPressed, input.IsSpecialActionPressed, input.IsInteractPressed,
                input.IsSprintPressed, input.IsJumpPressed, input.IsSwitchModePressed);
        }

        public static Player_Input ToProto(this InputState state, uint inputTick)
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

        public static Input_Sample ToInputSample(uint inputTick, in InputState state)
        {
            return new Input_Sample
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
        
        private static InputState ToInputState(Vec2 move, Vec2 aim, bool primaryAttack, bool specialAttack,
            bool specialAction, bool interact, bool sprint, bool jump, bool switchMode)
        {
            return new InputState
            {
                MoveInput = ToUnity(move),
                AimInput = ToUnity(aim),
                IsPrimaryAttackPressed = primaryAttack,
                IsSpecialAttackPressed = specialAttack,
                IsSpecialActionPressed = specialAction,
                IsInteractPressed = interact,
                IsSprintPressed = sprint,
                IsJumpPressed = jump,
                IsSwitchModePressed = switchMode,
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
                VerticalVelocity = movement.verticalVelocity,
                JumpCount = movement.jumpCount,
                IsRunning = movement.isRunning,
                IsSprinting = movement.isSprinting,
                IsDashing = movement.isDashing,
                IsGrounded = movement.isGrounded,
                DashDirection = ToProto(movement.dashDirection),
                DashRemainingTime = movement.dashRemainingTime,
                DesiredLocomotionSpeed = movement.desiredLocomotionSpeed,
                AppliedLocomotionSpeed = movement.appliedLocomotionSpeed,
                LastMoveDirection = ToProto(movement.lastMoveDirection),
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
                    verticalVelocity = snapshot.VerticalVelocity,
                    jumpCount = snapshot.JumpCount,
                    isRunning = snapshot.IsRunning,
                    isSprinting = snapshot.IsSprinting,
                    isDashing = snapshot.IsDashing,
                    isGrounded = snapshot.IsGrounded,
                    dashDirection = ToUnity(snapshot.DashDirection),
                    dashRemainingTime = snapshot.DashRemainingTime,
                    desiredLocomotionSpeed = snapshot.DesiredLocomotionSpeed,
                    appliedLocomotionSpeed = snapshot.AppliedLocomotionSpeed,
                    lastMoveDirection = ToUnity(snapshot.LastMoveDirection),
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
