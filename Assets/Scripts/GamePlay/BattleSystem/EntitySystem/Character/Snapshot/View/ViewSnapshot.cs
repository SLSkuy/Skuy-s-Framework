using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 视角旋转状态定义
    /// </summary>
    [Serializable]
    public struct ViewSnapshot
    {
        public Quaternion viewRotation;
        // 暂不外插，停用快照视角角速度。
        // public Vector3 viewAngularVelocity;
        
        public float yaw;
        public float pitch;
        
        public bool isFocus;
    }
}
