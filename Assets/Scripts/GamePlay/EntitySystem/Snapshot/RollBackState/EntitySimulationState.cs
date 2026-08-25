using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体最基础状态
    /// </summary>
    [Serializable]
    public struct EntitySimulationState
    {
        public uint entityState;    // 实体当前状态（待机、死亡等）
    }
}
