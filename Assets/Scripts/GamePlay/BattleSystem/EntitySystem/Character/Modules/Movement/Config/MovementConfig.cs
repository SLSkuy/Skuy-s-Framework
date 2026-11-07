using System;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    [Serializable]
    public class MovementConfig
    {
        [Header("移动属性")]
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public int jumpCount = 2;
        public float jumpSpeed = 12f;
        
        [Header("物理属性")]
        public float radius = 0.2f;
        public float height = 1.7f;
        public float gravity = 12f;
        public float maxFallSpeed = 20f;
        
        [Header("视觉属性")]
        [Min(0f)] public float meshTurnSpeed = 360f;   // mesh 转向由 orientation 映射的目标方向的角速度（度/秒）
    }
}