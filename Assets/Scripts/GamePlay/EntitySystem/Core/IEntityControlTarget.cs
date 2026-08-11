namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 控制器可驱动的实体目标。
    /// </summary>
    public interface IEntityControlTarget : IEntityIntentReceiver, IEntitySimulation, IEntityStateView
    {
    }
}

