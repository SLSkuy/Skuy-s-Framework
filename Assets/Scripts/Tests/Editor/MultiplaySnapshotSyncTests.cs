using Framework;
using GamePlay.EntitySystem;
using GamePlay.MultiPlaySystem;
using GamePlay.Simulator;
using NUnit.Framework;
using UnityEngine;
using Utils;

namespace Tests.Editor
{
    public sealed class MultiplaySnapshotSyncTests
    {
        [Test]
        public void AuthorityBuffer_OutOfOrderAndGap_FollowsWindow()
        {
            AuthorityInputProvider provider = new(8, 8);
            InputState first = new() { MoveInput = Vector2.up };
            InputState third = new() { MoveInput = Vector2.right };

            Assert.AreEqual(0u, provider.LastProcessedTick);
            Assert.AreEqual(Vector2.zero, provider.GetInputState().MoveInput);
            Assert.AreEqual(0u, provider.LastProcessedTick);

            Assert.IsTrue(provider.Enqueue(1, first));
            Assert.IsTrue(provider.Enqueue(3, third));
            Assert.IsFalse(provider.Enqueue(0, default));

            Assert.AreEqual(Vector2.up, provider.GetInputState().MoveInput);
            Assert.AreEqual(1u, provider.LastProcessedTick);
            Assert.AreEqual(Vector2.zero, provider.GetInputState().MoveInput);
            Assert.AreEqual(2u, provider.LastProcessedTick);
            Assert.AreEqual(Vector2.right, provider.GetInputState().MoveInput);
            Assert.AreEqual(3u, provider.LastProcessedTick);
        }

        [Test]
        public void AuthorityBuffer_FirstPacketAlignsConsumePoint()
        {
            AuthorityInputProvider provider = new(8, 8);
            InputState move = new() { MoveInput = Vector2.up };
            Assert.IsTrue(provider.Enqueue(40, move));
            Assert.AreEqual(Vector2.up, provider.GetInputState().MoveInput);
            Assert.AreEqual(40u, provider.LastProcessedTick);
        }

        [Test]
        public void MatchRoom_RejectsUnauthorizedAndClearsOnLeave()
        {
            MatchRoom room = new();
            Assert.IsTrue(room.TryJoin(1, out uint entityId));
            Assert.AreEqual(1u, entityId);
            Assert.IsTrue(room.TryAuthorize(1, entityId));
            Assert.IsFalse(room.TryAuthorize(2, entityId));
            Assert.IsTrue(room.TryLeave(1, out uint leftId));
            Assert.AreEqual(entityId, leftId);
            Assert.IsFalse(room.TryAuthorize(1, entityId));
            Assert.AreEqual(0, room.MemberCount);
        }

        [Test]
        public void SnapshotSyncCodec_RoundtripsInputAndRollbackFields()
        {
            InputState input = new()
            {
                MoveInput = new Vector2(0.5f, -1f),
                AimInput = new Vector2(0.25f, 0.75f),
                IsJumpPressed = true,
                IsSprintPressed = true
            };
            global::NetSync.Player_Input encoded = ProtoUtils.ToPlayerInput(7, 3, input);
            InputState decoded = ProtoUtils.ToInputState(encoded);
            Assert.AreEqual(7u, encoded.EntityId);
            Assert.AreEqual(3u, encoded.InputTick);
            Assert.AreEqual(input.MoveInput, decoded.MoveInput);
            Assert.AreEqual(input.AimInput, decoded.AimInput);
            Assert.IsTrue(decoded.IsJumpPressed);
            Assert.IsTrue(decoded.IsSprintPressed);

            EntityRollbackState state = new()
            {
                simulationState = new EntitySimulationState { entityState = 2 },
                movementState = new MovementRollbackState
                {
                    rootPosition = new Vector3(1f, 2f, 3f),
                    meshRotation = Quaternion.Euler(0f, 90f, 0f),
                    rootLinearVelocity = Vector3.forward,
                    meshAngularVelocity = Vector3.up,
                    isGrounded = true
                },
                viewState = new ViewRollbackState
                {
                    viewRotation = Quaternion.Euler(10f, 45f, 0f)
                }
            };
            global::NetSync.Character_Snapshot snapshot = ProtoUtils.ToCharacterSnapshotMessage(
                state, 7, 1, 12, 4);
            EntityRollbackState restored = ProtoUtils.ToRollbackState(snapshot);
            Assert.AreEqual(7u, snapshot.EntityId);
            Assert.AreEqual(12u, snapshot.SnapshotTick);
            Assert.AreEqual(4u, snapshot.LastProcessedInputTick);
            Assert.AreEqual(state.simulationState.entityState, restored.simulationState.entityState);
            Assert.AreEqual(state.movementState.rootPosition, restored.movementState.rootPosition);
            Assert.AreEqual(state.movementState.isGrounded, restored.movementState.isGrounded);
            Assert.AreEqual(state.viewState.viewRotation.eulerAngles.y, restored.viewState.yaw, 0.05f);
        }
    }
}
