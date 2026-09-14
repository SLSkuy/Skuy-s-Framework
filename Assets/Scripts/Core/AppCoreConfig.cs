using Framework;
using UnityEngine;
using YooAsset;

namespace Core
{
    /// <summary>
    /// 全局游戏设置
    /// </summary>
    [CreateAssetMenu(fileName = "AppCoreConfig", menuName = "AppCore/AppCoreConfig")]
    public class AppCoreConfig : ScriptableObjectSingleton<AppCoreConfig>
    {
        [Header("游戏基础设置")] 
        public int targetFrame = 60;
        
        [Header("资源加载")] 
        public EPlayMode resourceMode = EPlayMode.EditorSimulateMode;
        public float clearUnusedIdleTime = 30f;
        public string defaultPackageName = "DefaultPackage";
    }
}