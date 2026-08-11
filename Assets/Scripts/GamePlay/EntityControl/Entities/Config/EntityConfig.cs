using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [CreateAssetMenu(fileName = "EntityConfig", menuName = "GamePlay/EntityConfig")]
    public class EntityConfig : ScriptableObjectSingleton<EntityConfig>
    {
        [Header("基础实体属性")]
        public float walkSpeed = 3f;
        public float runSpeed = 5f;
        public float sprintSpeed = 8f;
        public int jumpCount = 2;
        public float jumpSpeed = 12f;
        public float dashSpeed = 20f;
        public float dashDuration = 0.3f;

        [Header("旋转属性")]
        [Min(0f)] public float aimHorizontalSpeed = 180f;
        [Min(0f)] public float aimVerticalSpeed = 120f;
        public float minAimPitch = -40f;
        public float maxAimPitch = 70f;
        [Min(0f)] public float meshTurnSpeed = 360f;   // 非锁定状态下模型转向移动方向的角速度（度/秒）

        [Header("动画属性")]
        public bool rootMotion = true;
        [Min(0f)] public float animSpeedSmoothTime = 0.1f;  // 动画 Speed 参数插值平滑时间（秒）

        [Header("物理属性")] 
        public float gravity = 12f;
        public float maxFallSpeed = 20f;
    }
}
