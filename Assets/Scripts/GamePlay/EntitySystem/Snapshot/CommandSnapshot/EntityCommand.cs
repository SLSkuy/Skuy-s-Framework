using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制命令抽象
    /// </summary>
    [Serializable]
    public struct EntityCommand
    {
        public uint tick;
        public Vector2 move;
        public Vector2 aim;
        [Tooltip("持续按住的命令按键集合")] public EntityCommandFlags flagsHeld;
        [Tooltip("当前Tick按下的命令按键集合")] public EntityCommandFlags flagsPressed;
        [Tooltip("当前Tick释放的命令按键集合")] public EntityCommandFlags flagsReleased;

        /// <summary>
        /// 判断当前控制命令有哪些持续按住的按键
        /// </summary>
        public readonly bool IsHeld(EntityCommandFlags flag) => (flagsHeld & flag) != 0;
        
        /// <summary>
        /// 判断当前控制命令在当前Tick有哪些按下的按键
        /// </summary>
        public readonly bool IsPressed(EntityCommandFlags flag) => (flagsPressed & flag) != 0;
        
        /// <summary>
        /// 判断当前控制命令在当前Tick有哪些释放的按键
        /// </summary>
        public readonly bool IsReleased(EntityCommandFlags flag) => (flagsReleased & flag) != 0;
    }
}
