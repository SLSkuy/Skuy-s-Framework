using Framework;
using GamePlay.Simulator;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class SimulatorAuthorityBufferTests
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
    }
}
