namespace Framework
{
    /// <summary>
    /// 子系统接口，定义每一个子系统都应有的方法，便于整体管理
    /// </summary>
    public interface ISubSystem
    {
        /// <summary>
        /// 子系统优先级
        /// </summary>
        SubSystemPriority Priority { get; }
        bool IsInitialized { get; }

        void _Init();
        void Update(float deltaTime);
        void LateUpdate();
        void FixedUpdate(float fixedDeltaTime);
        void _Destroy();
    }
}
