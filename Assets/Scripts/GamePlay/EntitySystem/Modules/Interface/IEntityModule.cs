namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体能力模块接口，用于装配移动、动画、交互、生命值等能力
    /// </summary>
    public interface IEntityModule
    {
        ModuleType ModuleType { get; }
        bool IsEnabled { get; }
        void Bind(EntityCharacter target);
        void SetEnabled(bool isEnabled);
        void Unbind();
    }
}
