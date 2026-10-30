using UnityEngine;

namespace GamePlay.BattleSystem
{
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "GamePlay/CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("基础运动属性")]
        public float walkSpeed = 2f;
        public float runSpeed = 5f;
        public float sprintSpeed = 8f;
        public int jumpCount = 2;
        public float jumpSpeed = 12f;

        [Header("视角控制属性")]
        [Min(0f)] public float aimHorizontalSpeed = 180f;
        [Min(0f)] public float aimVerticalSpeed = 120f;
        public float minAimPitch = -40f;
        public float maxAimPitch = 70f;
        
        [Header("表现属性")]
        public bool rootMotion = true;
        [Min(0f)] public float animSpeedSmoothTime = 0.1f;  // 水平速度向期望速度过渡的平滑时间（秒），移动与动画共用
        [Min(0f)] public float meshTurnSpeed = 360f;   // mesh 转向由 orientation 映射的目标方向的角速度（度/秒）

        [Header("物理属性")] 
        public float radius = 0.2f;
        public float height = 1.7f;
        public float gravity = 12f;
        public float maxFallSpeed = 20f;
    }
}
