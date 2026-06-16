using Framework.Core;

namespace Framework.Panel
{
    /// <summary>
    /// 面板界面属性
    /// </summary>
    public interface IPanelProperties : IUIProperties
    {
        PanelPriority Priority { get; set; }
    }
}