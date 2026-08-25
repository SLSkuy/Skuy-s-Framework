using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 移动回滚状态定义
    /// </summary>
    [Serializable]
    public struct MovementRollbackState
    {
        // 根节点位置
        public Vector3 rootPosition;
        public Vector3 rootLinearVelocity;
        
        // 模型旋转
        public Quaternion meshRotation;
        public Vector3 meshAngularVelocity;
        
        public Vector3 lastMoveDirection;
        public Vector3 dashDirection;
        
        public float appliedLocomotionSpeed;
        public float desiredLocomotionSpeed;
        public float verticalVelocity;
        
        public float dashRemainingTime;
        public int jumpCount;
        
        public bool isDashing;
        public bool isSprinting;
        public bool isRunning;
        public bool isGrounded;
    }
}
