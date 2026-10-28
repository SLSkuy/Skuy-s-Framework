using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    public class Simulator : IDisposable
    {
        private readonly SimulationDataProxy _simulationData = Global.GetDataProxy<SimulationDataProxy>();

        private readonly Dictionary<uint, EntityCommand> _playerCommands = new();    // 玩家输入
        private readonly SimulationEntityManager _entityManager = new();    // 模拟实体管理
        private TickSystem _simulationTickSystem;   // 模拟Tick
        private TickSystem _snapshotTickSystem;     // 快照Tick

        #region 属性
        public bool IsRunning => _simulationTickSystem is { IsRunning: true };
        
        public float TickDeltaTime => _simulationTickSystem?.TickDeltaTime ?? 0;
        public float InterpolationAlpha => _simulationTickSystem?.InterpolationAlpha ?? 0f;
        
        public IReadOnlyDictionary<uint, SimulationEntity> Entities => _entityManager.Entities;
        #endregion
        
        #region 事件
        /// <summary>
        /// 实行步长模拟前的Tick，用于逻辑位置插值准备
        /// </summary>
        public event Action PrepareSimulation;

        /// <summary>
        /// 收集本地输入
        /// </summary>
        public event Action<uint> CollectPlayerInput;
        
        /// <summary>
        /// 所有逻辑处理完之后，用于获取最新快照状态
        /// </summary>
        public event Action CaptureSnapshot;
        
        /// <summary>
        /// 捕获完快照之后，进行预测
        /// </summary>
        public event Action<uint, float> LocalPredict;

        /// <summary>
        /// 插值视觉表现
        /// </summary>
        public event Action PresentVisual;
        
        public event Action<uint, float> SnapshotTick;
        #endregion

        #region Tick管理

        public void StartServerTick()
        {
            StartSimulationTick();
            StartSnapshotTick();
        }

        public void StartClientTick()
        {
            StartSimulationTick();
        }

        public void StopTick()
        {
            _simulationTickSystem?.Stop();
            _snapshotTickSystem?.Stop();
        }

        private void StartSimulationTick()
        {
            _simulationTickSystem = new TickSystem(_simulationData.TickRate, _simulationData.MaxTickPerFrame);
            _simulationTickSystem.Tick += OnSimulationTick;
            _simulationTickSystem.Start();
        }

        private void StartSnapshotTick()
        {
            _snapshotTickSystem = new TickSystem(_simulationData.SnapshotSyncRate, _simulationData.MaxTickPerFrame);
            _simulationTickSystem.Tick += OnSnapshotTick;
            _snapshotTickSystem.Start();
        }
        
        private void OnSimulationTick(uint tick, float deltaTime)
        {
            PrepareSimulation?.Invoke();
            CollectPlayerInput?.Invoke(tick);
            
            CollectCommand();
            DoSimulator(tick, deltaTime);
            
            CaptureSnapshot?.Invoke();
            LocalPredict?.Invoke(tick, deltaTime);
        }

        private void OnSnapshotTick(uint tick, float deltaTime)
        {
            SnapshotTick?.Invoke(tick, deltaTime);
        }

        #endregion

        #region 模拟管理

        /// <summary>
        /// 收集模拟运行命令
        /// </summary>
        private void CollectCommand()
        {
            _playerCommands.Clear();
            foreach (KeyValuePair<uint, SimulationEntity> pair in _entityManager.Entities)
            {
                SimulationEntity entity = pair.Value;
                if(entity.Identity.IsReplica) continue; // 复制实体不进行步长模拟，只消耗权威快照
                if(!entity.TryCollectCommand(out EntityCommand command)) continue;  // 获取输入命令失败，跳过本次步进模拟
                _playerCommands.Add(pair.Key, command);
            }
        }

        /// <summary>
        /// 使用收集到的命令执行一次模拟
        /// </summary>
        private void DoSimulator(uint tick, float deltaTime)
        {
            foreach (KeyValuePair<uint, SimulationEntity> pair in _entityManager.Entities)
            {
                if(!_playerCommands.TryGetValue(pair.Key, out EntityCommand command)) continue;
                pair.Value.Character.Step(tick, deltaTime, command);
            }
        }

        #endregion

        #region 模拟实体管理

        public bool Register(PlayerIdentity identity, EntityCharacter character)
        {
            return _entityManager.Register(identity, character);
        }

        public bool Unregister(uint entityId)
        {
            return _entityManager.Unregister(entityId);
        }

        public bool TryGet(uint entityId, out SimulationEntity entity)
        {
            return _entityManager.TryGet(entityId, out entity);
        }

        public bool TryGet(uint entityId, out EntityIdentity identity, out EntityCharacter character)
        {
            if (TryGet(entityId, out SimulationEntity entity))
            {
                identity = entity.Identity;
                character = entity.Character;
                return true;
            }

            identity = null;
            character = null;
            return false;
        }

        /// <summary>
        /// 设置模拟实体的控制命令来源
        /// </summary>
        public bool SetInputSource(uint entityId, IInputProvider inputSource)
        {
            if (!TryGet(entityId, out SimulationEntity entity)) return false;
            
            entity.SetInputSource(inputSource);
            return true;
        }

        #endregion

        #region 生命周期

        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            _simulationTickSystem?.Update(deltaTime);
            _snapshotTickSystem?.Update(deltaTime);
            PresentVisual?.Invoke();
        }

        public void Dispose()
        {
            StopTick();
            
            if (_simulationTickSystem != null)
            {
                _simulationTickSystem.Tick -= OnSimulationTick;
                _simulationTickSystem = null;
            }

            if (_snapshotTickSystem != null)
            {
                _snapshotTickSystem.Tick -= OnSnapshotTick;
                _snapshotTickSystem = null;
            }
            
            _entityManager.Dispose();
            _playerCommands.Clear();
            
            PrepareSimulation = null;
            CollectPlayerInput = null;
            CaptureSnapshot = null;
            SnapshotTick = null;
        }

        #endregion
    }
}