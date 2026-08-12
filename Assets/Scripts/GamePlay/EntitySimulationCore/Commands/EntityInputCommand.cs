using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 一个固定模拟 Tick 消费的完整实体输入命令。
    /// </summary>
    [Serializable]
    public struct EntityInputCommand
    {
        public uint Tick;
        public Vector2 Move;
        public Vector2 Aim;
        public EntityCommandButtons ButtonsHeld;
        public EntityCommandButtons ButtonsPressedThisTick;

        /// <summary>
        /// 判断指定按钮在当前 Tick 是否持续按住。
        /// </summary>
        public bool IsHeld(EntityCommandButtons button)
        {
            return (ButtonsHeld & button) != 0;
        }

        /// <summary>
        /// 判断指定按钮是否在当前 Tick 产生按下边沿。
        /// </summary>
        public bool IsPressed(EntityCommandButtons button)
        {
            return (ButtonsPressedThisTick & button) != 0;
        }
    }
}
