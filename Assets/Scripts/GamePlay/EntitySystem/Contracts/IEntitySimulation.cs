namespace GamePlay.EntitySystem
{
    public interface IEntitySimulation
    {
        void Step(uint tick, float deltaTime, in EntityCommand command);
    }
}
