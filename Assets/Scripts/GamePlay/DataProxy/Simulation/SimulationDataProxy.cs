using Framework;
using GamePlay.Simulation;
using YooAsset;

namespace GamePlay.DataProxy
{
    public class SimulationDataProxy : BaseDataProxy
    {
        public override string DataPath => "Config_SimulationConfig";
        public override bool IsNeedSaveToLocal => false;

        private AssetHandle _handle;
        private SimulationConfig _config;

        #region 属性
        public int TickRate => _config ? _config.simulationTickRate : 60;
        public int MaxTickPerFrame => _config ? _config.maxSimulationTicksPerFrame : 8;

        public int SnapshotSyncRate => _config ? _config.snapshotTickRate : 30;
        public int InterpolateDelayTick => _config ? _config.interpolationDelayTicks : 3;
        public int MaxBufferedInputs => _config ? _config.maxBufferedInputs : 32;
        public int MaxFutureInputs => _config ? _config.maxFutureInputTicks : 32;

        public int MaxPredictionTicks => _config ? _config.predictionHistorySize : 64;
        public float PositionReconcileThreshold => _config ? _config.positionReconcileThreshold : 0.05f;
        public float RotationReconcileThreshold => _config ? _config.rotationReconcileThresholdDegrees : 1f;
        #endregion

        public override void Load()
        {
            _handle = Global.Load<SimulationConfig>("Config_SimulationConfig");
            _config = _handle.AssetObject as  SimulationConfig;
        }

        public override void Destroy()
        {
            _config = null;
            
            _handle?.Dispose();
            _handle = null;
        }
    }
}