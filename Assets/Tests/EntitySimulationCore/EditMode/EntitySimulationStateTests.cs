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

        [Test]
        public void RejectsNonFiniteVelocityAndZeroRotation()
        {
            EntitySimulationState infiniteVelocity = new()
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                LinearVelocity = new Vector3(float.PositiveInfinity, 0f, 0f)
            };
            EntitySimulationState zeroRotation = new()
            {
                Position = Vector3.zero,
                Rotation = new Quaternion(0f, 0f, 0f, 0f)
            };

            Assert.That(infiniteVelocity.IsFinite(), Is.False);
            Assert.That(zeroRotation.IsFinite(), Is.False);
        }

        [Test]
        public void AcceptsFinitePoseAndVelocity()
        {
            EntitySimulationState state = new()
            {
                Position = new Vector3(1f, 2f, 3f),
                Rotation = Quaternion.identity,
                LinearVelocity = Vector3.forward,
                AngularVelocity = Vector3.up
            };

            Assert.That(state.IsFinite(), Is.True);
        }
    }
}

