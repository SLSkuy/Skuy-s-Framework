using Framework;
using GamePlay.NetSync;
using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家控制器，需要访问到EntityCharacter组件进行控制
    /// 桥接输入源和玩家实体
    /// </summary>
    public class ClientPlayerController : AutoEventMonoBehaviour
    {
        /// <summary>
        /// 预测帧状态
        /// </summary>
        private struct PredictFrame
        {
            public uint InputTick;
            public InputState Input;
            public NetPlayerSnapshot Snapshot;
        }
        
        private IPlayerCharacter _character;
        private IInputStateProvider _inputProvider;
        private NetworkVisualSmoother _visualSmoother;
        private Transform _cameraTransform;

        /// <summary>
        /// 缓存当前原始移动输入
        /// </summary>
        private InputState _currentInputState;
        
        // 预测状态缓存
        private PredictFrame[] _predictFrames;
        private uint _lastProcessedInputTick;
        private uint _currentInputTick;

        public void Configure(IPlayerCharacter character, IInputStateProvider inputProvider,
            NetworkVisualSmoother visualSmoother)
        {
            _character = character;
            _inputProvider = inputProvider;
            _visualSmoother = visualSmoother;
        }
        
        /// <summary>
        /// 设置相机Transform
        /// </summary>
        /// <param name="cameraTrans">相机Transform</param>
        public void SetCameraTransform(Transform cameraTrans)
        {
            _cameraTransform = cameraTrans;
        }

        /// <summary>
        /// 设置输入来源
        /// </summary>
        /// <param name="provider"></param>
        public void SetInputStateProvider(IInputStateProvider provider)
        {
            _inputProvider = provider;
        }

        /// <summary>
        /// 获取映射后的输入方向
        /// </summary>
        /// <returns>映射后的方向</returns>
        private Vector2 GetMappedDirection(Vector2 inputDir)
        {
            if (!_cameraTransform)
            {
                return inputDir;
            }
            
            return TransformUtils.MapInputToWorldDirection2D(inputDir, _cameraTransform);
        }

        #region 网络同步

        /// <summary>
        /// 本地输入上传Tick，触发预测逻辑
        /// </summary>
        public InputState OnInputTick(uint inputTick, float deltaTime)
        {
            InputState input = _inputProvider.GetInputState();
            input.MoveInput = GetMappedDirection(input.MoveInput);
            
            // 记录当前输入Tick，用于接收权威状态后判断预测了多少Tick
            _currentInputTick = inputTick;
            _currentInputState = input;
            
            Predict(inputTick, deltaTime);

            return input;
        }

        /// <summary>
        /// 预测，立刻响应本地输入
        /// </summary>
        private void Predict(uint inputTick, float deltaTime)
        {
            // 立刻模拟移动
            _character.Move(_currentInputState.MoveInput, deltaTime);

            // 防止还未初始化完毕
            if (_predictFrames == null) return;
            
            // 记录当前预测对应的信息
            int index = (int)(inputTick % (uint)_predictFrames.Length);
            _predictFrames[index] = new PredictFrame
            {
                InputTick = inputTick,
                Input = _currentInputState,
                Snapshot = _character.CaptureSnapshot(),
            };
        }

        /// <summary>
        /// 接收到权威状态
        /// </summary>
        public void OnAuthoritySnapshot(NetPlayerSnapshot snapshot, float deltaTime)
        {
            uint authorityTick = snapshot.LastProcessedInputTick;
            if (authorityTick <= _lastProcessedInputTick) return;
            _lastProcessedInputTick = authorityTick;

            // 获取预测快照
            NetPlayerSnapshot predict = _character.CaptureSnapshot();

            _visualSmoother?.CaptureBeforeCorrection();

            // 应用快照状态
            _character.ApplySnapshot(snapshot);
            
            // 开始回放未确认的输入
            Reply(deltaTime);

            // 获取权威修正后的重放快照
            NetPlayerSnapshot replayedSnapshot = _character.CaptureSnapshot();
            
            // 开始和解
            Reconciliation(predict, replayedSnapshot);
        }

        /// <summary>
        /// 开始回放未确认的输入
        /// </summary>
        private void Reply(float deltaTime)
        {
            // 重演服务端未确认Tick
            for (uint inputTick = _lastProcessedInputTick + 1; inputTick <= _currentInputTick; inputTick++)
            {
                int index = (int)(inputTick % (uint)_predictFrames.Length);
                PredictFrame frame = _predictFrames[index];
            
                if (frame.InputTick != inputTick)
                    break;
            
                // 更新模拟状态
                _character.Move(frame.Input.MoveInput, deltaTime);
                frame.Snapshot = _character.CaptureSnapshot();
                _predictFrames[index] = frame;
            }
        }

        /// <summary>
        /// 和解，计算预测状态与权威状态间的差距，进行处理
        /// </summary>
        private void Reconciliation(NetPlayerSnapshot predict, NetPlayerSnapshot replayedSnapshot)
        {
            _visualSmoother?.ApplyCorrectionOffset(predict, replayedSnapshot);
        }
        
        #endregion

        #region 生命周期

        protected override void Start()
        {
            int capacity = Mathf.Max(2, SyncConfig.Instance.maxBufferedInputs);
            _predictFrames = new PredictFrame[capacity];

            // 如果没有设置相机，自动查找主相机
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }
            
            // 相机不要跟随Tick驱动的权威位置，而是跟随插值驱动的渲染节点，减少卡顿
            Transform cameraTarget = _visualSmoother != null ? _visualSmoother.VisualRoot : transform;
            Global.Get<CameraManager>()?.SetTarget(cameraTarget);
            
            base.Start();
        }
        
        #endregion
    }
}
