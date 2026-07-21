using UnityEngine;
using Framework;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 服务端玩家控制器，控制服务端玩家对象，远端输入接收载体，抽象事件控制实体
    /// 桥接输入源和玩家实体
    /// </summary>
    public class ServerPlayerController : AutoEventMonoBehaviour
    {
        public bool IsReady { get; private set; }

        private IPlayerCharacter _serverPlayerCharacter;
        private IInputStateProvider _serverInputProvider;

        // 接收转换后的输入，避免服务端获取摄像机转换，过于麻烦
        private Vector2 _lastTickInput;
        private Vector2 _currentTickInput;
        
        // 输入缓存，采用循环队列的形式保存
        private InputState[] _inputStates;
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
            _serverPlayerCharacter = serverPlayerCharacter;
            _serverInputProvider = serverInputProvider;
        }

        /// <summary>
        /// 使用Tick间隔模拟，避免帧率不同导致模拟结果不一致
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            // 通过Tick驱动获取输入变化，再触发内部事件
            // 若在设置输入状态时直接触发，会扰乱原本的Tick驱动顺序
            _serverInputProvider.CheckDiffFromLastState();
            _serverPlayerCharacter.Move(_currentTickInput, deltaTime);
        }

        #region 事件订阅

        // TODO: 先保证最小同步原型，之同步位置和旋转，不做其他的同步
        [AutoEvent("OnMove", nameof(_serverInputProvider))]
        private void OnMoveEvent(Vector2 moveInput)
        {
            _lastTickInput = _currentTickInput;
            _currentTickInput = moveInput;
        }

        #endregion
    }
}