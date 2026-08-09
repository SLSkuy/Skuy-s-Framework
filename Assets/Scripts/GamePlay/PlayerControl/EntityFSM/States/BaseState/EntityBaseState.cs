using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体最基础状态，持有 EntityContext，提供所有状态共有属性
    /// </summary>
    public abstract class EntityBaseState : ExtendableStateBase<uint>
    {
        protected readonly EntityContext Context;

        #region 状态属性
        protected EntityConfig Config => Context.Config;
        protected EntityMotor Motor => Context.Motor;
        #endregion

        protected EntityBaseState(EntityContext context) : base(context.StateMachine)   // 通用基类依旧只看到状态机
        {
            Context = context;
        }
    }
}
