using UnityEngine;

namespace Framework
{
    public interface IInputProvider
    {
        /// <summary>
        /// 移动输入朝向
        /// </summary>
        Vector2 MoveInput { get; }
        
        /// <summary>
        /// 瞄准输入朝向
        /// </summary>
        Vector2 AimInput { get; }
        
        /// <summary>
        /// 普通攻击输入
        /// </summary>
        bool IsPrimaryAttackPressed { get; }
        
        /// <summary>
        /// 特殊攻击输入
        /// </summary>
        bool IsSpecialAttackPressed { get; }

        /// <summary>
        /// 特殊操作输入
        /// </summary>
        bool IsSpecialActionPressed { get; }
        
        /// <summary>
        /// 交互操作输入
        /// </summary>
        bool IsInteractPressed { get; }
        
        /// <summary>
        /// 疾跑输入
        /// </summary>
        bool IsSprintPressed { get; }
        
        /// <summary>
        /// 跳跃输入
        /// </summary>
        bool IsJumpPressed { get; }
    }
}
