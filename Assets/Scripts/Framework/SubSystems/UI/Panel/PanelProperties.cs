using System;
using UnityEngine;

namespace Framework
{
    [Serializable]
    public class PanelProperties : IPanelProperties
    {
        [SerializeField] [Tooltip("当期面板优先级，决定渲染优先级")]
        private PanelPriority priority;
        
        #region 暴露属性
        
        public PanelPriority Priority { get => priority; set => priority = value; }
        
        #endregion

        // 供预制体上的 UIView 序列化使用
        public PanelProperties()
        {
        }

        public PanelProperties(PanelPriority priority)
        {
            this.priority = priority;
        }
    }
}