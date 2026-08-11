namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体控制器接口，用于描述控制源与实体目标的绑定关系。
    /// </summary>
    public interface IEntityController
    {
        /// <summary>
        /// 当前绑定的实体目标。
        /// </summary>
        IEntityControlTarget Target { get; }

        /// <summary>
        /// 绑定实体目标。
        /// </summary>
        void Bind(IEntityControlTarget target);

        /// <summary>
        /// 解绑实体目标。
        /// </summary>
        void Unbind();
    }
}

