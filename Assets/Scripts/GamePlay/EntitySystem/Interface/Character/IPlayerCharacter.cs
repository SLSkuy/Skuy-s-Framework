namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家角色实体接口，定义玩家角色实体能够进行哪些操作
    /// </summary>
    public interface IPlayerCharacter : IEntityCharacter
    {
        /// <summary>
        /// 朝当前角色朝向进行基础攻击
        /// </summary>
        void PrimaryAttack();
        void StopPrimaryAttack();
        
        /// <summary>
        /// 朝当前角色朝向进行特殊攻击
        /// </summary>
        void SpecialAttack();
        void StopSpecialAttack();
        
        /// <summary>
        /// 朝当前角色朝向进行特殊操作
        /// </summary>
        void SpecialAction();
        void StopSpecialAction();
        
        /// <summary>
        /// 与当前选中的物品进行交互
        /// </summary>
        void Interact();
        void StopInteract();
    }
}