using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟属性设置
    /// </summary>
    [CreateAssetMenu(fileName = "SimulationConfig", menuName = "GamePlay/MultiPlay/SimulationConfig")]
    public class SimulationConfig : ScriptableObject
    {
        [Header("权威模拟")]
        [Min(1)] public int simulationTickRate = 60;
        [Min(1)] public int maxSimulationTicksPerFrame = 8;
        
        [Header("状态快照")]
        [Min(1)] public int snapshotTickRate = 30;
        [Min(0)] public int interpolationDelayTicks = 3;
        [Min(1)] public int maxBufferedInputs = 32;
        [Min(0)] public int maxFutureInputTicks = 32;

        [Header("客户端预测")]
        [Min(2)] public int predictionHistorySize = 64;
        [Tooltip("角色位置差距插值过渡阈值")][Min(0f)] public float positionReconcileThreshold = 0.02f;
        [Tooltip("角色旋转差距插值过渡阈值")][Min(0f)] public float rotationReconcileThresholdDegrees = 1f;
        [Tooltip("角色位置差距强制拉回阈值")][Min(0f)] public float positionSnapThreshold = 2f;
        [Tooltip("角色旋转差距强制拉回阈值")][Min(0f)] public float rotationSnapThresholdDegrees = 45f;
    }
}
