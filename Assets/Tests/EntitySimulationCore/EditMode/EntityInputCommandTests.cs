using NUnit.Framework;
using UnityEngine;

namespace GamePlay.EntitySystem.Tests
{
    public class EntityInputCommandTests
    {
        [Test]
        public void SeparatesHeldButtonsFromPressedEdges()
        {
            EntityInputCommand command = new()
            {
                ButtonsHeld = EntityCommandButtons.Sprint | EntityCommandButtons.Jump,
                ButtonsPressedThisTick = EntityCommandButtons.Jump
            };

            Assert.That(command.IsHeld(EntityCommandButtons.Sprint), Is.True);
            Assert.That(command.IsHeld(EntityCommandButtons.Jump), Is.True);
            Assert.That(command.IsPressed(EntityCommandButtons.Jump), Is.True);
            Assert.That(command.IsPressed(EntityCommandButtons.Sprint), Is.False);
        }

        [Test]
        public void DefaultCommandIsNoOpInput()
        {
            EntityInputCommand command = default;

            Assert.That(command.Move, Is.EqualTo(Vector2.zero));
            Assert.That(command.Aim, Is.EqualTo(Vector2.zero));
            Assert.That(command.ButtonsHeld, Is.EqualTo(EntityCommandButtons.None));
            Assert.That(command.ButtonsPressedThisTick, Is.EqualTo(EntityCommandButtons.None));
        }
    }
}
