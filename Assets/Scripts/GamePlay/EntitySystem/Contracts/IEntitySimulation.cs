namespace GamePlay.EntitySystem
{
    public interface IEntitySimulation
    {
        void Step(uint tick, float deltaTime, in EntityInputCommand command);
    }
}
