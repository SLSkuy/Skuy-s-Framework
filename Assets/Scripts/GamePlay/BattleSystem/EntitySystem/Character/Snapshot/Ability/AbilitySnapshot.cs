using System;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public struct AbilitySnapshot
    {
        public uint skillId;
        public float elapsedTime;
    }
}