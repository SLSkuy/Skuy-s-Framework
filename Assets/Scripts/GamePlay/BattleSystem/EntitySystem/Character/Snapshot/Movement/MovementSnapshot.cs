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
        public Vector3 rootLinearVelocity;
        
        // 模型旋转
        public Quaternion meshRotation;
        public Vector3 meshAngularVelocity;
        
        public Vector3 lastMoveDirection;
        
        public float appliedLocomotionSpeed;
        public float desiredLocomotionSpeed;
        public float verticalVelocity;
        
        public int jumpCount;
        
        public bool isSprinting;
        public bool isRunning;
        public bool isGrounded;
    }
}
