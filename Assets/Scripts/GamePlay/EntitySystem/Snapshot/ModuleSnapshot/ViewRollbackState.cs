using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 视角旋转状态定义
    /// </summary>
    [Serializable]
    public struct ViewRollbackState
    {
        public Quaternion viewRotation;
        public Vector3 viewAngularVelocity;
        
        public float yaw;
        public float pitch;
        
        public bool isFocus;
    }
}
