namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体能力模块接口，用于装配移动、动画、交互、生命值等能力。
    /// </summary>
    public interface IEntityModule
    {
        /// <summary>
        /// 组件类型
        /// </summary>
        ModuleType ModuleType { get; }
        
        /// <summary>
        /// 模块是否启用。
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 绑定实体目标。
        /// </summary>
        void Bind(EntitySimulationObject target);

        /// <summary>
        /// 设置启用状态。
        /// </summary>
        void SetEnabled(bool isEnabled);

        /// <summary>
        /// 解绑实体目标。
        /// </summary>
        void Unbind();
    }
}
