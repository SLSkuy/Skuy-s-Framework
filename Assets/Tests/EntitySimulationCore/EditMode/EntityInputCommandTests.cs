using NUnit.Framework;

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
    }
}
