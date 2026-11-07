using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public class ViewConfig
    {
        [Min(0f)] public float aimHorizontalSpeed = 180f;
        [Min(0f)] public float aimVerticalSpeed = 120f;
        public float minAimPitch = -40f;
        public float maxAimPitch = 70f;
    }
}