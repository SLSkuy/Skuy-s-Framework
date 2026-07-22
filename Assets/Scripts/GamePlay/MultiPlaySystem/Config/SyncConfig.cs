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
        [Header("权威模拟")]
        [Min(1)] public int simulationTickRate = 32;
        
        [Header("状态快照")]
        [Min(1)] public int snapshotTickRate = 20;
        [Min(0)] public int interpolationDelayTicks = 3;
        [Min(1)] public int maxBufferedInputs = 64;
    }
}
