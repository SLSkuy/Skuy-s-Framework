using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 移动回滚状态定义
    /// </summary>
    [Serializable]
    public struct MovementSnapshot
    {
        // 根节点位置
        public Vector3 rootPosition;
        // 暂不外插，停用快照线速度和角速度。
        // public Vector3 rootLinearVelocity;
        
        // 模型旋转
        public Quaternion meshRotation;
        // public Vector3 meshAngularVelocity;
        
        public Vector3 lastMoveDirection;
        
        /// <summary>
        /// 模拟期望水平速度，空中沿用起跳前的值
        /// </summary>
        public float locomotionSpeed;
        public float verticalVelocity;
        
        // 这几个状态字段对于客户端的主控玩家来说有用，但是对于快照来说没啥任何作用
        // 考虑后续动画系统是否会使用，暂时继续同步该字段
        public int jumpCount;
        public bool isSprinting;
        public bool isRunning;
        public bool isGrounded;
    }
}
