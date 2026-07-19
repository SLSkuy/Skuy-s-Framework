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

        [Header("客户端命令")]
        [Min(1)] public int commandTickRate = 20;
        
        [Header("状态快照")]
        [Min(1)] public int snapshotTickRate = 20;
        [Min(1)] public int interpolationDelayTicks = 3;    // 快照缓存数量，防止不缓存直接开始插值快照但未来快照还未送达导致实体直接停止
        [Min(1)] public int maxBufferedInputs = 64;
    }
}
