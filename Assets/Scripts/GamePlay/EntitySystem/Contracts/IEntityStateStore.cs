namespace GamePlay.EntitySystem
{
    public interface IEntityStateStore
    {
        EntitySimulationState CaptureSimulationState();
        EntityRollbackState CaptureRollbackState();
        void RestoreRollbackState(in EntityRollbackState state);
    }
}
