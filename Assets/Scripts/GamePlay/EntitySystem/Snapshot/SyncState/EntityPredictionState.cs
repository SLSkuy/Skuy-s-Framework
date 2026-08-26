using System;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体预测状态数据缓存
    /// </summary>
    [Serializable]
    public struct EntityPredictionState
    {
        public uint tick;
        public EntityCommand command;
        public EntityRollbackState state;
    }
}
