using System.Collections.Generic;

namespace GamePlay.Simulation
{
    public class ClientFramePrediction
    {
        private PredictionFrameBuffer _frameBuffer;
        private List<EntityPredictionFrame> _frames;
        private Simulator _simulator;

        public ClientFramePrediction(int maxFrameCount, Simulator simulator)
        {
            _frameBuffer = new PredictionFrameBuffer(maxFrameCount);
            _frames = new List<EntityPredictionFrame>();
            _simulator = simulator;
        }

        /// <summary>
        /// 执行一次预测，缓存预测后的状态
        /// </summary>
        public void Predict(uint inputTick)
        {
            
        }

        /// <summary>
        /// 接收到权威状态，执行回滚，并重放操作
        /// </summary>
        public void ReceiveAuthorityFrame(uint inputTick, in EntityAuthorityFrame frame)
        {
            
        }
    }
}