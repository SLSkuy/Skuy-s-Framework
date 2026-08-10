using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 输入状态提供器接口，定义玩家的输入数据结构获取方法
    /// </summary>
    public interface IInputStateProvider : IInputProvider
    {
        /// <summary>
        /// 移动输入事件
        /// </summary>
        event Action<Vector2> OnMove;
        
        /// <summary>
        /// 瞄准输入事件
        /// </summary>
        event Action<Vector2> OnAim;
        
        /// <summary>
        /// 普通攻击按下事件
        /// </summary>
        event Action OnPrimaryAttackPressed;
        
        /// <summary>
        /// 普通攻击释放事件
        /// </summary>
        event Action OnPrimaryAttackReleased;
        
        /// <summary>
        /// 特殊攻击按下事件
        /// </summary>
        event Action OnSpecialAttackPressed;
        
        /// <summary>
        /// 特殊攻击释放事件
        /// </summary>
        event Action OnSpecialAttackReleased;
        
        /// <summary>
        /// 特殊操作按下事件
        /// </summary>
        event Action OnSpecialActionPressed;
        
        /// <summary>
        /// 特殊操作释放事件
        /// </summary>
        event Action OnSpecialActionReleased;
        
        /// <summary>
        /// 交互按下事件
        /// </summary>
        event Action OnInteractPressed;
        
        /// <summary>
        /// 交互释放事件
        /// </summary>
        event Action OnInteractReleased;
        
        /// <summary>
        /// 疾跑按下事件
        /// </summary>
        event Action OnSprintPressed;
        
        /// <summary>
        /// 疾跑释放事件
        /// </summary>
        event Action OnSprintReleased;
        
        /// <summary>
        /// 冲刺按下事件
        /// </summary>
        event Action OnJump;

        /// <summary>
        /// 行走模式切换
        /// </summary>
        event Action OnSwitchModePressed;
        
        /// <summary>
        /// 获取当前的输入状态
        /// </summary>
        /// <returns>输入状态</returns>
        InputState GetInputState();
        
        /// <summary>
        /// 应用输入状态给当前提供器
        /// </summary>
        /// <param name="state"></param>
        void SetInputState(InputState state);
        
        /// <summary>
        /// 检查上一状态与当前状态的差距并触发对应的事件
        /// </summary>
        void CheckDiffFromLastState();
    }
}