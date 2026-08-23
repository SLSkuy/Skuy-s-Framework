using System.Collections.Generic;
using NUnit.Framework;

namespace GamePlay.EntitySystem.Tests
{
    public class EntityPredictionHistoryTests
    {
        [Test]
        public void ReplacesFramesAndCopiesOnlyUnconfirmedCommands()
        {
            EntityPredictionHistory history = new(8);
            history.Add(new EntityPredictionFrame { Tick = 1 });
            history.Add(new EntityPredictionFrame { Tick = 2 });
            history.Add(new EntityPredictionFrame { Tick = 2, Command = new EntityInputCommand { Tick = 2 } });
            history.Add(new EntityPredictionFrame { Tick = 3 });

            Assert.That(history.Count, Is.EqualTo(3));
            Assert.That(history.TryGet(2, out EntityPredictionFrame confirmed), Is.True);
            Assert.That(confirmed.Command.Tick, Is.EqualTo(2));

            List<EntityPredictionFrame> replay = new();
            history.CopyAfter(2, replay);
            Assert.That(replay.Count, Is.EqualTo(1));
            Assert.That(replay[0].Tick, Is.EqualTo(3));
        }

        [Test]
        public void RemovesConfirmedFramesThroughTick()
        {
            EntityPredictionHistory history = new(4);
            history.Add(new EntityPredictionFrame { Tick = 5 });
            history.Add(new EntityPredictionFrame { Tick = 6 });
            history.Add(new EntityPredictionFrame { Tick = 7 });

            history.RemoveThrough(6);

            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history.LatestTick, Is.EqualTo(7));
        }

        [Test]
        public void ConfirmationBoundaryKeepsOnlyUnconfirmedReplay()
        {
            EntityPredictionHistory history = new(8);
            history.Add(new EntityPredictionFrame { Tick = 10 });
            history.Add(new EntityPredictionFrame { Tick = 11 });
            history.Add(new EntityPredictionFrame { Tick = 12 });

            List<EntityPredictionFrame> replay = new();
            history.CopyAfter(10, replay);
            history.RemoveThrough(10);

            Assert.That(replay.Count, Is.EqualTo(2));
            Assert.That(replay[0].Tick, Is.EqualTo(11));
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.TryGet(10, out _), Is.False);
        }
    }
}
