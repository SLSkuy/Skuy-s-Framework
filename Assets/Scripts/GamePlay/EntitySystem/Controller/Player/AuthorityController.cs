using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 服务端权威驱动器（普通 C# 类，由 ServerSimulator 持有）。
    /// 接收客户端上传输入→排队→每 Tick 消费一条→模拟→产出快照。
    /// 算法沿用旧版 ServerPlayerController，模拟入口改为 EntityCharacter.Simulate。
    /// </summary>
    [RequireComponent(typeof(EntityCharacter))]
    [RequireComponent(typeof(NetEntitySyncRoot))]
    public class AuthorityController : EntityControllerBase
    {
        /// <summary>
        /// 待处理输入
        /// </summary>
        private struct PendingInput
        {
            public uint Tick;
            public InputState State;
        }

        private EntityCharacter _character;
        private NetEntitySyncRoot _syncRoot;
        private InputState _previousInput;

        // 接收转换后的输入，按 Tick 顺序消费
        private readonly Queue<PendingInput> _pendingInputs = new();
        private uint _latestReceivedInputTick;
        private uint _lastProcessedInputIndex;

        #region 属性
        public override EntityDriveMode DriveMode => EntityDriveMode.Authority;
        public bool IsReady { get; private set; }
        #endregion
        
        /// <summary>
        /// 配置服务器模拟玩家数据源
        /// </summary>
        public void Init(EntityCharacter character, NetEntitySyncRoot syncRoot)
        {
            IsReady = true;
            _character = character;
            Bind(character);
            _syncRoot = syncRoot;
            Target.TickDrive = true;
        }

        /// <summary>
        /// 接收客户端上传输入
        /// </summary>
        public void ReceiveInput(uint inputTick, InputState inputState)
        {
            if (!IsReady || inputTick <= _latestReceivedInputTick) return;

            _latestReceivedInputTick = inputTick;
            _pendingInputs.Enqueue(new PendingInput { Tick = inputTick, State = inputState });
        }
        
        /// <summary>
        /// 使用 Tick 间隔模拟，每个 Tick 只消费一条输入，
        /// 确保确认 Tick 与客户端预测/重放的模拟步数一一对应。
        /// </summary>
        public void Simulate(float tickDeltaTime)
        {
            if (!IsReady || Target == null) return;
            if (_pendingInputs.Count == 0) return;

            PendingInput pendingInput = _pendingInputs.Dequeue();

            // 写入输入并模拟
            EntityControllerUtils.ApplyTo(Target, pendingInput.State, ref _previousInput);
            Target.Simulate(tickDeltaTime);

            // 只有在本次服务端模拟完成后，才能向客户端确认该输入
            _lastProcessedInputIndex = pendingInput.Tick;
        }

        public NetPositionSnapshot CapturePositionSnapshot(uint simulationTick)
        {
            if (!TryGetPositionSync(out NetPositionSync positionSync)) return default;

            return positionSync.CaptureSnapshot(simulationTick, _lastProcessedInputIndex);
        }

        private bool TryGetPositionSync(out NetPositionSync positionSync)
        {
            positionSync = null;
            return _syncRoot != null && _syncRoot.TryGetModule(ModuleType.Position, out positionSync);
        }

        private void Awake()
        {
            _character = GetComponent<EntityCharacter>();
            _syncRoot = GetComponent<NetEntitySyncRoot>();

            Bind(_character);
        }
    }
}
