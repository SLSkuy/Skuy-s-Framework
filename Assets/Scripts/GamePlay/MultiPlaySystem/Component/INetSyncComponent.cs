namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步组件接口，声明组件基础信息
    /// </summary>
    public interface INetSyncComponent
    {
        /// <summary>
        /// 同步组件ID
        /// </summary>
        SyncModuleID ModuleId { get; }
        
        /// <summary>
        /// 根据网络角色配置同步组件行为
        /// </summary>
        void ConfigureRole(NetEntityRole role);
    }
}
