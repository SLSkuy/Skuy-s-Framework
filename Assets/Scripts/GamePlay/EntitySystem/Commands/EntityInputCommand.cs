using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct EntityInputCommand
    {
        public uint Tick;
        public Vector2 Move;
        public Vector2 Aim;
        public EntityCommandButtons ButtonsHeld;
        public EntityCommandButtons ButtonsPressedThisTick;

        public bool IsHeld(EntityCommandButtons button) => (ButtonsHeld & button) != 0;
        public bool IsPressed(EntityCommandButtons button) => (ButtonsPressedThisTick & button) != 0;
    }
}
