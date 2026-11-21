using UnityEngine;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 角色的一块受击区域
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class HurtBox : MonoBehaviour
    {
        private EntityCharacter _owner;
        private Collider _collider;
        
        #region 属性
        /// <summary>当前层级中拥有此受击区域的角色</summary>
        public EntityCharacter Owner => _owner;
        #endregion

        #region 生命周期

        private void Awake()
        {
            _owner = GetComponentInParent<EntityCharacter>();
            _collider = GetComponent<Collider>();
            
            _collider.isTrigger = true;
        }
            
        #endregion
    }
}
