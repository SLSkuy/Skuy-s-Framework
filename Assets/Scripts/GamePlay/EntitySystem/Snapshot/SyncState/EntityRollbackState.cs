using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体对象整体状态集合
    /// </summary>
    [Serializable]
    public struct EntityRollbackState
    {
        public EntitySimulationState simulationState;
        public MovementRollbackState movementState;
        public ViewRollbackState viewState;
    }
}
