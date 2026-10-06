using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体最基础状态快照
    /// </summary>
    [Serializable]
    public struct StateSnapshot
    {
        public EntityState entityState;    // 实体当前状态（待机、死亡等）
    }
}
