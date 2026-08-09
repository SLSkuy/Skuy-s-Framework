using UnityEngine;
using System.Collections.Generic;
using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 服务端玩家控制器，控制服务端玩家对象，远端输入接收载体，抽象事件控制实体
    /// 桥接输入源和玩家实体
    /// </summary>
    public class ServerPlayerController : AutoEventMonoBehaviour
    {
        private struct PendingInput
        {
            public uint Tick;
            public InputState State;
        }
        
        public bool IsReady { get; private set; }

        private IPlayerCharacter _character;
        private IInputStateProvider _serverInputProvider;

        // 接收转换后的输入，避免服务端获取摄像机转换，过于麻烦
        private InputState _currentTickInput;
        
        // 输入缓存，暂不使用
        private readonly Queue<PendingInput> _pendingInputs = new();
        private uint _latestReceivedInputTick;
        private uint _lastProcessedInputIndex;
        
        // 状态缓存，保存最近1s的状态快照，用于后续延迟补偿，采用循环队列的形式保存，暂不使用
        private NetPlayerSnapshot[] _netPlayerSnapshots;
        private uint _lastProcessedSnapshotIndex;
        
        /// <summary>
        /// 配置服务器模拟玩家数据源
        /// </summary>
        public void Configure(IPlayerCharacter serverPlayerCharacter, IInputStateProvider serverInputProvider)
        {
            IsReady = true;
            _character = serverPlayerCharacter;
            _serverInputProvider = serverInputProvider;
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
        /// 使用Tick间隔模拟，避免帧率不同导致模拟结果不一致
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            if (!IsReady || _character == null) return;

            // 每个服务端模拟 Tick 只消费一条客户端命令，确保确认 Tick
            // 与客户端预测/重放的模拟步数保持一一对应。
            if (_pendingInputs.Count == 0) return;

            PendingInput pendingInput = _pendingInputs.Dequeue();
            _currentTickInput = pendingInput.State;
            _serverInputProvider.SetInputState(pendingInput.State);
            
            // 模拟当前输入
            SimulateCharacter(_currentTickInput, deltaTime);
            
            // 只有在本次服务端模拟完成后，才能向客户端确认该输入。
            _lastProcessedInputIndex = pendingInput.Tick;
        }

        public NetPlayerSnapshot CaptureSnapshot(uint simulationTick)
        {
            return _character.CaptureSnapshot(simulationTick, _lastProcessedInputIndex);
        }

        #region 事件订阅
        
        private void SimulateCharacter(InputState inputState, float deltaTime)
        {
            OnMove(inputState.MoveInput, deltaTime);
            OnAim(inputState.AimInput);
        }

        // TODO: 先保证最小同步原型，之同步位置和旋转，不做其他的同步
        private void OnMove(Vector2 moveInput, float deltaTime)
        {
            _character.Move(moveInput, deltaTime);
        }

        private void OnAim(Vector2 aimInput)
        {
            Vector3 aimDirection = new Vector3(aimInput.x, 0f, aimInput.y);
            _character.Aim(aimDirection);
        }

        #endregion
    }
}
