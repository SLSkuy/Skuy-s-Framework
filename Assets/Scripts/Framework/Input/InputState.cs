using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 输入状态定义，抽象玩家输入数据，后续网络同步时，只需要同步该数据接口即可
    /// </summary>
    [Serializable]
    public struct InputState : IInputProvider
    {
        public Vector2 MoveInput { get; set; }
        public Vector2 AimInput { get; set; }
        public bool IsPrimaryAttackPressed { get; set; }
        public bool IsSpecialAttackPressed { get; set; }
        public bool IsSpecialActionPressed { get; set; }
        public bool IsInteractPressed { get; set; }
        public bool IsSprintPressed { get; set; }
        public bool IsDashPressed { get; set; }

        public InputState(IInputProvider provider)
        {
            MoveInput = provider.MoveInput;
            AimInput = provider.AimInput;
            IsPrimaryAttackPressed = provider.IsPrimaryAttackPressed;
            IsSpecialAttackPressed = provider.IsSpecialAttackPressed;
            IsSpecialActionPressed = provider.IsSpecialActionPressed;
            IsInteractPressed = provider.IsInteractPressed;
            IsSprintPressed = provider.IsSprintPressed;
            IsDashPressed = provider.IsDashPressed;
        }
    }
}