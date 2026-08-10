using Framework;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端本地玩家控制器，负责控制客户端对应角色
    /// </summary>
    [RequireComponent(typeof(EntityCharacter))]
    [RequireComponent(typeof(NetTransformSync))]
    public class LocalController : MonoBehaviour
    {
        /// <summary>
        /// 预测帧状态
        /// </summary>
        private struct PredictFrame
        {
            public uint InputTick;
            public InputState Input;
            public NetTransformSnapshot Snapshot;
        }
        
        private IInputStateProvider _inputProvider;
        private EntityCharacter _character;
        private NetTransformSync _transformSync;

        // 预测处理
        private InputState _currentInput;
        private InputState _previousInput;
        private PredictFrame[] _predictFrames;
        private uint _lastProcessedInputTick;
        private uint _currentInputTick;

        // 是否为单机
        private bool _isLocalPlay;
        
        // 初始化本地控制器
        public void Init(IInputStateProvider inputProvider, bool localPlay)
        {
            _isLocalPlay = localPlay;
            _inputProvider = inputProvider;
            
            int capacity = Mathf.Max(2, SyncConfig.Instance.maxBufferedInputs);
            _predictFrames = new PredictFrame[capacity];
            
            // 确保没有重复订阅
            Unsubscribe();
            Subscribe();
        }

        #region 网络同步

        /// <summary>
        /// 捕获当前输入，由外部统一Tick驱动时调用
        /// </summary>
        public InputState CaptureInput(uint inputTick, float tickTime)
        {
            InputState input = _inputProvider.GetInputState();
            
            _currentInputTick = inputTick;
            _currentInput = input;
            
            Predict(inputTick, tickTime);

            return input;
        }

        /// <summary>
        /// 预测，立刻响应本地输入
        /// </summary>
        private void Predict(uint inputTick, float tickTime)
        {
            // 立刻响应输入
            NetDriverInput.ApplyTo(_character, _currentInput, ref _previousInput);
            _character.Simulate(tickTime);
            
            // 防止还未初始化完毕
            if (_predictFrames == null) return;

            uint index = inputTick % (uint)_predictFrames.Length;
            _predictFrames[index] = new PredictFrame
            {
                InputTick = inputTick,
                Input = _currentInput,
                Snapshot = _transformSync.CaptureSnapshot(inputTick)
            };
        }

        /// <summary>
        /// 接受权威状态，由外部统一Tick时调用
        /// </summary>
        public void OnAuthoritySnapshot(NetTransformSnapshot snapshot, float tickTime)
        {
            uint authorityTick = snapshot.LastProcessedInputTick;
            if (authorityTick <= _lastProcessedInputTick) return;
            _lastProcessedInputTick = authorityTick;

            // 防止还未初始化完毕
            if (_predictFrames == null) return;
            int index = (int)(_currentInputTick % (uint)_predictFrames.Length);

            // 获取预测快照
            NetTransformSnapshot predictSnapshot = _predictFrames[index].Snapshot;

            // 应用快照状态
            _transformSync.ApplySnapshot(snapshot);

            // 重置边沿检测基线为权威 Tick 那一帧的输入（若仍在缓冲内），
            // 否则回退 default，避免回放第一帧边沿检测错误
            int authIndex = (int)(authorityTick % (uint)_predictFrames.Length);
            _previousInput = _predictFrames[authIndex].InputTick == authorityTick ?
                _predictFrames[authIndex].Input : default;

            // 开始回放未确认的输入
            Reply(tickTime);

            // 获取权威修正后的重放快照
            NetTransformSnapshot replayedSnapshot = _predictFrames[index].Snapshot;

            // 开始和解
            Reconciliation(predictSnapshot, replayedSnapshot);
        }

        /// <summary>
        /// 重放
        /// </summary>
        private void Reply(float tickDeltaTime)
        {
            // 重演服务端未确认 Tick
            for (uint inputTick = _lastProcessedInputTick + 1; inputTick <= _currentInputTick; inputTick++)
            {
                int index = (int)(inputTick % (uint)_predictFrames.Length);
                PredictFrame frame = _predictFrames[index];

                if (frame.InputTick != inputTick)
                    break;

                // 更新模拟状态
                NetDriverInput.ApplyTo(_character, frame.Input, ref _previousInput);
                _character.Simulate(tickDeltaTime);

                frame.Snapshot = _transformSync.CaptureSnapshot(inputTick);
                _predictFrames[index] = frame;
            }
        }
        
        /// <summary>
        /// 和解，计算预测状态与权威状态间的差距，进行处理
        /// </summary>
        private void Reconciliation(NetTransformSnapshot predictSnapshot, NetTransformSnapshot replayedSnapshot)
        {
            // 暂时不使用smoother插值
        }

        #endregion

        #region 实体控制

        private void Subscribe()
        {
            if (!_isLocalPlay || _inputProvider == null) return;
            
            _inputProvider.OnMove += Move;
            _inputProvider.OnAim += Aim;
            _inputProvider.OnJump += Jump;
            _inputProvider.OnSprintPressed += SprintPressed;
            _inputProvider.OnSprintReleased += SprintReleased;
            _inputProvider.OnSwitchModePressed += SwitchModePressed;
        }

        private void Unsubscribe()
        {
            if(!_isLocalPlay || _inputProvider == null) return;
            
            _inputProvider.OnMove -= Move;
            _inputProvider.OnAim -= Aim;
            _inputProvider.OnJump -= Jump;
            _inputProvider.OnSprintPressed -= SprintPressed;
            _inputProvider.OnSprintReleased -= SprintReleased;
            _inputProvider.OnSwitchModePressed -= SwitchModePressed;
        }
        
        private void Move(Vector2 move) => _character.Move(move);
        private void Aim(Vector2 aim) => _character.Aim(aim);
        private void Jump() => _character.Jump();
        private void SprintPressed() => _character.StartSprint();
        private void SprintReleased() => _character.StopSprint();
        private void SwitchModePressed() => _character.ToggleRun();

        #endregion

        #region 生命周期

        private void Awake()
        {
            _character = GetComponent<EntityCharacter>();
            _transformSync = GetComponent<NetTransformSync>();
        }

        private void Start()
        {
            // ===== 测试代码 =====
            Init(GetComponent<LocalInputProvider>(), true);
            Cursor.lockState = CursorLockMode.Locked;
            Global.Get<CameraManager>().SetTarget(transform.Find("orientation"));
            // ===== 测试代码 =====
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion
    }
}
