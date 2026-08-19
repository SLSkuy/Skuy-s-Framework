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
        [Min(1)] public int maxSimulationTicksPerFrame = 8;
        
        [Header("状态快照")]
        [Min(1)] public int snapshotTickRate = 20;
        [Min(0)] public int interpolationDelayTicks = 3;
        [Min(1)] public int maxBufferedInputs = 64;
        [Min(0)] public int maxPastInputTicks = 32;
        [Min(0)] public int maxFutureInputTicks = 2;
        [Min(0f)] public float maxInputVectorMagnitude = 1f;

        [Header("客户端预测")]
        [Min(2)] public int predictionHistorySize = 128;
        [Min(0f)] public float positionReconcileThreshold = 0.02f;
        [Min(0f)] public float rotationReconcileThresholdDegrees = 1f;
        [Min(0f)] public float positionSnapThreshold = 2f;
        [Min(0f)] public float rotationSnapThresholdDegrees = 45f;
    }
}
