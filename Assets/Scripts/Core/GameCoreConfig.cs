using Framework;
using UnityEngine;
using YooAsset;

namespace Core
{
    /// <summary>
    /// 全局游戏设置
    /// </summary>
    [CreateAssetMenu(fileName = "GameCoreConfig", menuName = "GameCore/GameCoreConfig")]
    public class GameCoreConfig : ScriptableObjectSingleton<GameCoreConfig>
    {
        [Header("游戏基础设置")] 
        public int targetFrame = 60;
        
        [Header("资源加载")] 
        public float clearUnusedIdleTime = 30f;
        public EPlayMode resourceMode = EPlayMode.EditorSimulateMode;
        public string defaultPackageName = "DefaultPackage";
    }
}