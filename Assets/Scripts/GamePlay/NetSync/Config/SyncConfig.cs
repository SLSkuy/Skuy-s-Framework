using Framework;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络同步属性配置
    /// </summary>
    [CreateAssetMenu(fileName = "SyncConfig", menuName = "GamePlay/NetSync/SyncConfig")]
    public class SyncConfig : ScriptableObjectSingleton<SyncConfig>
    {
        [Header("客户端同步属性配置")] 
        public int commandTickRate = 20;
        
        [Header("同步属性配置")]
        public int snapShotTickRate = 128;
    }
}