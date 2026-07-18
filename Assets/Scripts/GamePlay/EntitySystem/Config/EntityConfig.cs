using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [CreateAssetMenu(fileName = "EntityConfig", menuName = "GamePlay/EntityConfig")]
    public class EntityConfig : ScriptableObjectSingleton<EntityConfig>
    {
        [Header("基础实体属性")] 
        public float walkSpeed = 5f;
        public float sprintSpeed = 10f;
        public float dashSpeed = 20f;
        public float dashDuration = 0.3f;
    }
}