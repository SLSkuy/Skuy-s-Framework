using System.Collections.Generic;

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
        /// 接收到权威状态，执行回滚，并重放操作
        /// </summary>
        public void ReceiveAuthorityFrame(uint inputTick, in EntityAuthorityFrame frame)
        {
            
        }

        /// <summary>
        /// 清空已记录的预测帧。
        /// </summary>
        public void Clear()
        {
            _buffers.Clear();
            _frames.Clear();
        }
    }
}
