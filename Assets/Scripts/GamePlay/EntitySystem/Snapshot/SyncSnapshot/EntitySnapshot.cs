using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体对象整体状态快照集合
    /// </summary>
    [Serializable]
    public struct EntitySnapshot
    {
        public StateSnapshot state;
        public MovementSnapshot movement;
        public ViewSnapshot view;
    }
}
