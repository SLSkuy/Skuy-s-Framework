using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 客户端预测历史。按输入 Tick 保存主控实体已执行的命令和执行后的快照。
    /// </summary>
    public sealed class ClientFramePrediction
    {
        private readonly List<EntityPredictionFrame> _frames;
        private readonly PredictionFrameBuffer _buffers;
        private readonly Simulator _simulator;

        public ClientFramePrediction(int maxFrameCount, Simulator simulator)
        {
            _buffers = new PredictionFrameBuffer(maxFrameCount);
            _frames = new List<EntityPredictionFrame>();
            _simulator = simulator;
        }

        /// <summary>
        /// 本拍模拟完成后，记下主控实体执行过的命令和完整快照。
        /// </summary>
        public void Predict(uint inputTick)
        {
            EntityPredictionFrame frame = _simulator.CapturePredictedFrames(inputTick);
            _buffers.AddFrame(frame);
        }

        /// <summary>
        /// 用主机已处理的输入 Tick 对齐预测历史，确认点为 0 表示主机还没消费过该玩家输入
        /// </summary>
        public void ReceiveAuthorityFrame(uint entityId, uint lastProcessedTick, in EntityAuthorityFrame frame)
        {
            if (lastProcessedTick == 0) return;

            EntitySnapshot authority = frame.state;
            // 这一拍不在历史里：写入权威状态，丢掉该确认点及之前的记录，再重放仍留着的后续命令。
            if (!_buffers.TryGetFrame(lastProcessedTick, out EntityPredictionFrame local))
            {
                Restore(entityId, authority);
                _buffers.GetFramesAfter(lastProcessedTick, _frames);
                _buffers.RemoveFramesUntil(lastProcessedTick);
                Replay(entityId);
                return;
            }

            // 位置和朝向都在阈值内：预测仍然有效，只丢掉已确认的历史。
            if (CheckThreshold(local.state, authority))
            {
                _buffers.RemoveFramesUntil(lastProcessedTick);
                return;
            }

            // 超出阈值：模拟回到权威状态，用已保存的命令重放确认点之后的输入。
            Restore(entityId, authority);
            _buffers.GetFramesAfter(lastProcessedTick, _frames);
            _buffers.RemoveFramesUntil(lastProcessedTick);
            Replay(entityId);
        }

        /// <summary>
        /// 清空已记录的预测帧。
        /// </summary>
        public void Clear()
        {
            _buffers.Clear();
            _frames.Clear();
        }
        
        /// <summary>
        /// 把给定状态写回模拟，不改画面采样（回滚）
        /// </summary>
        private void Restore(uint entityId, in EntitySnapshot state)
        {
            _simulator.TryRestoreEntityState(entityId, state);
        }

        /// <summary>
        /// 按模拟 Tick 间隔重放 _frames 里的已存命令，并把新快照写回缓冲（重放）
        /// </summary>
        private void Replay(uint entityId)
        {
            if (!_simulator.TryGet(entityId, out _, out EntityCharacter character) || !character.IsInitialized) return;

            float stepDeltaTime = _simulator.TickDeltaTime;
            for (int i = 0; i < _frames.Count; i++)
            {
                EntityPredictionFrame predicted = _frames[i];
                character.Step(predicted.tick, stepDeltaTime, predicted.command);
                predicted.state = character.CaptureSnapshot();
                _buffers.AddFrame(predicted);
            }
        }
        
        /// <summary>
        /// 位置、身体朝向和视角都在和解阈值内时返回 true
        /// </summary>
        private bool CheckThreshold(in EntitySnapshot local, in EntitySnapshot authority)
        {
            float positionThreshold = _simulator.PositionReconcileThreshold;
            Vector3 delta = local.movement.rootPosition - authority.movement.rootPosition;
            if (delta.sqrMagnitude > positionThreshold * positionThreshold) return false;

            float rotationThreshold = _simulator.RotationReconcileThresholdDegrees;
            if (Quaternion.Angle(local.movement.meshRotation, authority.movement.meshRotation) > rotationThreshold) return false;
            if (Quaternion.Angle(local.view.viewRotation, authority.view.viewRotation) > rotationThreshold) return false;
            return true;
        }
    }
}
