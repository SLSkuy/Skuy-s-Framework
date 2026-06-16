using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    [Serializable]
    public struct UISettingsEntry
    {
        public bool isEnableOnRegister;
        public string uiName;
    }
    
    /// <summary>
    /// 单个场景的UI界面配置，例如玩家相关，载具相关，基础设置等
    /// </summary>
    [CreateAssetMenu(fileName = "New Config", menuName = "UIConfig")]
    public class UIConfig : ScriptableObject
    {
        [Header("场景UI配置")]
        [Tooltip("单个相关的UI场景名称")]public string uiSceneName;
        public List<UISettingsEntry> uiToRegister;
    }
}