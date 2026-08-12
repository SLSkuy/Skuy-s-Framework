using GamePlay.NetSync;
using NUnit.Framework;

namespace GamePlay.EntitySystem.Tests
{
    public class NetworkTickSystemTests
    {
        [Test]
        public void DifferentFrameRatesProduceSameTickCount()
        {
            NetworkTickSystem sixtyFps = new(30);
            NetworkTickSystem twentyFps = new(30);
            sixtyFps.Start();
            twentyFps.Start();

            for (int i = 0; i < 60; i++) sixtyFps.Advance(1d / 60d);
            for (int i = 0; i < 20; i++) twentyFps.Advance(1d / 20d);

            Assert.That(sixtyFps.CurrentTick, Is.EqualTo(30));
            Assert.That(twentyFps.CurrentTick, Is.EqualTo(30));
        }

        [Test]
        public void CatchUpLimitDropsExcessWholeTicksAndRecordsMetric()
        {
            NetworkTickSystem ticks = new(20, 3);
            ticks.Start();

            int processed = ticks.Advance(1d);

            Assert.That(processed, Is.EqualTo(3));
            Assert.That(ticks.CurrentTick, Is.EqualTo(3));
            Assert.That(ticks.CatchUpLimitCount, Is.EqualTo(1));
        }
    }
}
