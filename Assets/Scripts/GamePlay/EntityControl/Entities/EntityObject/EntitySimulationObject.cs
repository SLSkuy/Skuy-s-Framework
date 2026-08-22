using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 需要按固定 Tick 执行模拟的实体对象。
    /// 不包含网络同步实现，网络层通过模拟与状态接口接入。
    /// </summary>
    public abstract class EntitySimulationObject : EntityObject<EntityConfig, EntityContext>,
        IEntitySimulation, IEntityStateStore, IEntityStateView
    {
        private IEntityObjectIdentity _identity;

        #region 属性
        public uint EntityId => _identity != null ? _identity.EntityId : 0;
        public bool HasIdentity => _identity != null && _identity.IsInitialized;
        public abstract uint CurrentState { get; }
        #endregion

        public abstract void Step(uint tick, float deltaTime, in EntityInputCommand command);
        public abstract EntitySimulationState CaptureSimulationState();
        public abstract EntityRollbackState CaptureRollbackState();
        public abstract void RestoreRollbackState(in EntityRollbackState state);

        protected override void InitConfig()
        {
            _config ??= EntityConfig.Instance;
        }

        protected override void InitComponents()
        {
            _identity = GetComponent<IEntityObjectIdentity>();
        }

        protected override void InitContext()
        {
        }
    }
}
