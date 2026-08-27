using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 注册模拟对象实体，组合实体的身份和角色属性
    /// </summary>
    public struct RegisteredEntity
    {
        #region 属性
        public EntityObjectIdentity Identity { get; }
        public EntityCharacter Character { get; }
        #endregion
        
        public RegisteredEntity(EntityObjectIdentity identity, EntityCharacter character)
        {
            Identity = identity;
            Character = character;
        }
    }
}