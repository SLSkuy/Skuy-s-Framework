using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 输入状态定义，抽象玩家输入数据，后续网络同步时，只需要同步该数据接口即可
    /// </summary>
    [Serializable]
    public struct InputCommand
    {
        public Vector2 MoveInput { get; set; }
        public Vector2 AimInput { get; set; }

        /// <summary>
        /// 当前采样时持续按住的按键集合，按下和释放边沿由命令构建器计算。
        /// </summary>
        public EntityCommandFlags FlagsHeld { get; set; }

        /// <summary>
        /// 判断当前输入是否包含指定的按住标志。
        /// </summary>
        public readonly bool IsHeld(EntityCommandFlags flag) => (FlagsHeld & flag) != 0;
    }
}