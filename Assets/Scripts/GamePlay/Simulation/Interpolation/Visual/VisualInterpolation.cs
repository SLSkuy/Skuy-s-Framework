using System;
using System.Collections.Generic;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 本地主控玩家/主机玩家，插值画面表现
    /// 逻辑位置保持Tick驱动
    /// </summary>
    public class VisualInterpolation : IDisposable
    {
        private readonly Simulator _simulator;

        public VisualInterpolation(Simulator simulator)
        {
            _simulator = simulator;
            _simulator.PrepareSimulation += PrepareSimulationTick;
            _simulator.CaptureSnapshot += CaptureAuthorityTick;
            _simulator.PresentVisual += PresentVisualPoses;
        }

        private bool ShouldPresent(SimulationEntity entity)
        {
            if (!entity.Character.IsInitialized) return false;
            if(entity.Identity.IsReplica) return false;

            return true;
        }

        /// <summary>
        /// 获取模拟步进前的Tick状态
        /// </summary>
        private void PrepareSimulationTick()
        {
            foreach (KeyValuePair<uint, SimulationEntity> pair in _simulator.Entities)
            {
                SimulationEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.PrepareSimulationTick();
            }
        }

        /// <summary>
        /// 获取模拟步进后的Tick状态
        /// </summary>
        private void CaptureAuthorityTick()
        {
            foreach (KeyValuePair<uint, SimulationEntity> pair in _simulator.Entities)
            {
                SimulationEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.CaptureAuthorityAfterTick();
            }
        }

        /// <summary>
        /// 插值步进流程
        /// </summary>
        private void PresentVisualPoses()
        {
            float alpha = _simulator.InterpolationAlpha;
            foreach (KeyValuePair<uint, SimulationEntity> pair in _simulator.Entities)
            {
                SimulationEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.PresentVisualPose(alpha);
            }
        }

        public void Dispose()
        {
            _simulator.PrepareSimulation -= PrepareSimulationTick;
            _simulator.CaptureSnapshot -= CaptureAuthorityTick;
            _simulator.PresentVisual -= PresentVisualPoses;
        }
    }
}