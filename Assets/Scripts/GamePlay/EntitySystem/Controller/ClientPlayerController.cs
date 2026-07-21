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
        private IPlayerCharacter _character;
        private IInputStateProvider _inputProvider;
        private Transform _cameraTransform;

        /// <summary>
        /// 缓存当前原始移动输入
        /// </summary>
        private Vector2 _currentTickInput;
        
        // 预测状态缓存，每一个输入Tick对应一个快照Tick
        // 权威处理输入Tick下发后，能够获取到对应的预测状态
        // 采用循环队列，通过取余获取索引
        private InputState[] _inputStates;
        private NetPlayerSnapshot[] _snapshots;
        private int _maxInputBuffer;
        private uint _lastProcessedInputIndex;
        private uint _currentPredictInputIndex;

        public void Configure(IPlayerCharacter character, IInputStateProvider inputProvider)
        {
            _character = character;
            _inputProvider = inputProvider;

            int capacity = Mathf.Max(2, GamePlay.NetSync.SyncConfig.Instance.maxBufferedInputs);
            _inputStates = new InputState[capacity];
            _snapshots = new NetPlayerSnapshot[capacity];
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
            InputState inputState = _inputProvider.GetInputState();
            
            // 映射移动方向
            _currentTickInput = GetMappedDirection(inputState.MoveInput);
            inputState.MoveInput = _currentTickInput;
            
            // 记录输入序列及对应的角色状态
            int index = (int)(inputTick % (uint)_inputStates.Length);
            _currentPredictInputIndex = inputTick;
            _inputStates[index] = inputState;
            _snapshots[index] = _character.CaptureSnapshot(inputTick);
            
            return inputState;
        }

        /// <summary>
        /// 本地模拟Tick，触发预测逻辑
        /// </summary>
        public void OnSimulateTick(uint simTick, float deltaTime)
        {
            _character.Move(_currentTickInput, deltaTime);
        }

        /// <summary>
        /// 接收到权威状态
        /// </summary>
        /// <param name="snapshot"></param>
        public void OnAuthoritySnapshot(NetPlayerSnapshot snapshot)
        {
            _character.ApplySnapshot(snapshot);
            
            // Reconciliation(snapshot);
        }

        /// <summary>
        /// 预测与权威状态和解计算
        /// </summary>
        private void Reconciliation(NetPlayerSnapshot snapshot)
        {
            // 更新已确认的输入序号
            _lastProcessedInputIndex = snapshot.LastProcessedInputTick;

            int index = (int)(_lastProcessedInputIndex % (uint)_inputStates.Length);
            NetPlayerSnapshot predictSnapshot = _snapshots[index];
            
            // TODO: 重跑剩余的预测输入
        }

        #endregion

        #region 生命周期

        protected override void Start()
        {
            _maxInputBuffer = SyncConfig.Instance.maxBufferedInputs;
            _inputStates = new InputState[_maxInputBuffer];
            _snapshots = new NetPlayerSnapshot[_maxInputBuffer];
            
            // 如果没有设置相机，自动查找主相机
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }
            
            Global.Get<CameraManager>()?.SetTarget(transform);
            
            base.Start();
        }
        
        #endregion
    }
}
