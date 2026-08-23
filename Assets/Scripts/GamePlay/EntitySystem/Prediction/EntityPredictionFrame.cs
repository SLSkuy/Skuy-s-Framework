using System;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct EntityPredictionFrame
    {
        public uint Tick;
        public EntityInputCommand Command;
        public EntityRollbackState State;
    }
}
