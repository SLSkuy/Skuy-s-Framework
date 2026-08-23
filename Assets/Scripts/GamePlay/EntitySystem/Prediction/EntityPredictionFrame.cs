using System;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct EntityPredictionFrame
    {
        public uint Tick;
        public EntityCommand Command;
        public EntityRollbackState State;
    }
}
