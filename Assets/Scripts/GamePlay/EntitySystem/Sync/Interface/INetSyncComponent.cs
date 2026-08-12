namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步组件接口，声明组件基础信息。
    /// </summary>
    public interface INetSyncComponent
    {
        /// <summary>
        /// 同步组件ID。
        /// </summary>
        ModuleType ModuleType { get; }

        /// <summary>
        /// 绑定网络标识
        /// </summary>
        /// <param name="root"></param>
        void Bind(NetEntitySyncRoot root);

        /// <summary>
        /// 根据网络角色配置同步组件行为。
        /// </summary>
        void ConfigureRole(NetEntityRole role);
        
        /// <summary>
        /// 同步帧更新。
        /// </summary>
        void Update(float deltaTime);
    }
}
