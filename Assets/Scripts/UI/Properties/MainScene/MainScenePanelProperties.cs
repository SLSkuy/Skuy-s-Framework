using System;
using Framework;

namespace UI
{
    /// <summary>
    /// MainScenePanel 的界面数据，打开界面时传入
    /// </summary>
    [Serializable]
    public class MainScenePanelProperties : PanelProperties
    {
        // TODO: 补上该界面需要的数据字段，加上 SerializeField 即可在 UIView 上配置

        // 供预制体上的 UIView 序列化使用
        public MainScenePanelProperties() { }

        public MainScenePanelProperties(PanelPriority priority) : base(priority){ }
    }
}
