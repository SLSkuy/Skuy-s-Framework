using NUnit.Framework;
using UnityEngine;

namespace GamePlay.EntitySystem.Tests
{
    public class EntitySimulationStateTests
    {
        [Test]
        public void RejectsNonFiniteTransformState()
        {
            EntitySimulationState state = new()
            {
                Position = new Vector3(float.NaN, 0f, 0f),
                Rotation = Quaternion.identity
            };

            Assert.That(state.IsFinite(), Is.False);
        }
    }
}
