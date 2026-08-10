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
        
        [Header("动画属性")]
        public bool rootMotion = true;

        [Header("物理属性")] 
        public float gravity = 12f;
        public float maxFallSpeed = 20f;
    }
}
