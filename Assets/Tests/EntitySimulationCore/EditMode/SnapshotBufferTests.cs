using NUnit.Framework;

namespace GamePlay.EntitySystem.Tests
{
    public class SnapshotBufferTests
    {
        private struct TestSnapshot : IEntitySnapshot
        {
            public uint SnapshotTick { get; set; }
            public int Value;
        }

        [Test]
        public void StoresSnapshotsInTickOrderAndReplacesDuplicates()
        {
            SnapshotBuffer<TestSnapshot> buffer = new(4);

            buffer.Add(new TestSnapshot { SnapshotTick = 3, Value = 30 });
            buffer.Add(new TestSnapshot { SnapshotTick = 1, Value = 10 });
            buffer.Add(new TestSnapshot { SnapshotTick = 2, Value = 20 });
            buffer.Add(new TestSnapshot { SnapshotTick = 2, Value = 21 });

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer.OldestTick, Is.EqualTo(1));
            Assert.That(buffer.LatestTick, Is.EqualTo(3));
            Assert.That(buffer.TrySample(0.2d, 0.1d, out TestSnapshot from, out TestSnapshot to, out float t), Is.True);
            Assert.That(from.Value, Is.EqualTo(21));
            Assert.That(to.Value, Is.EqualTo(30));
            Assert.That(t, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void CapacityEvictsOldestSnapshot()
        {
            SnapshotBuffer<TestSnapshot> buffer = new(2);
            buffer.Add(new TestSnapshot { SnapshotTick = 1 });
            buffer.Add(new TestSnapshot { SnapshotTick = 2 });
            buffer.Add(new TestSnapshot { SnapshotTick = 3 });

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.OldestTick, Is.EqualTo(2));
            Assert.That(buffer.LatestTick, Is.EqualTo(3));
        }
    }
}
