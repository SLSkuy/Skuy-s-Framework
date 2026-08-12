using GamePlay.NetSync;
using NUnit.Framework;

namespace GamePlay.EntitySystem.Tests
{
    public class EntityCommandQueueTests
    {
        [Test]
        public void RejectsDuplicateAndAlreadyProcessedTicks()
        {
            EntityCommandQueue<int> queue = new(4);

            Assert.That(queue.Enqueue(2, 2, 2, 2, 20), Is.True);
            Assert.That(queue.Enqueue(2, 2, 2, 2, 21), Is.False);
            Assert.That(queue.TryDequeueExecutable(2, out uint tick, out int value), Is.True);
            Assert.That(tick, Is.EqualTo(2));
            Assert.That(value, Is.EqualTo(20));
            Assert.That(queue.Enqueue(1, 2, 2, 2, 10), Is.False);
        }

        [Test]
        public void RejectsCommandsAfterCapacityLimit()
        {
            EntityCommandQueue<int> queue = new(2);

            Assert.That(queue.Enqueue(1, 1, 0, 2, 10), Is.True);
            Assert.That(queue.Enqueue(2, 1, 0, 2, 20), Is.True);
            Assert.That(queue.Enqueue(3, 1, 0, 2, 30), Is.False);
        }

        [Test]
        public void FutureCommandWaitsUntilItsServerTick()
        {
            EntityCommandQueue<int> queue = new(4);

            Assert.That(queue.Enqueue(5, 3, 2, 2, 50), Is.True);
            Assert.That(queue.TryDequeueExecutable(3, out _, out _), Is.False);
            Assert.That(queue.LastProcessedTick, Is.EqualTo(0));
            Assert.That(queue.TryDequeueExecutable(5, out uint tick, out int value), Is.True);
            Assert.That(tick, Is.EqualTo(5));
            Assert.That(value, Is.EqualTo(50));
        }

        [Test]
        public void RejectsOutsideTickWindow()
        {
            EntityCommandQueue<int> queue = new(4);

            Assert.That(queue.Enqueue(0, 10, 2, 2, 0), Is.False);
            Assert.That(queue.Enqueue(13, 10, 2, 2, 13), Is.False);
        }
    }
}
