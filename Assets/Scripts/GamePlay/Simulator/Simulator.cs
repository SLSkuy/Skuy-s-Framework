using System;
using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;
using YooAsset;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 模拟核：持有唯一 Tick、实体注册表与步进调度。由 Host 子系统持有并驱动。
    /// </summary>
    public sealed class Simulator
    {
        private AssetHandle _configHandle;
        private TickSystem _tickSystem;
        private EntityRegistry _entityRegistry;
        private readonly Dictionary<uint, EntityCommand> _tickCommands = new();

        #region 属性
        public uint CurrentTick => _tickSystem?.CurrentTick ?? 0;
        public bool IsRunning => _tickSystem is { IsRunning: true };
        #endregion

        #region 事件
        /// <summary>
        /// 核时钟推进后转发；在本拍 Dispatch 之后触发，禁止再 new TickSystem。
        /// </summary>
        public event Action<uint, float> Tick;
        #endregion

        #region Tick管理

        public void StartClock()
        {
            if (IsRunning) return;
            _tickSystem.Start();
        }

        public void StopClock()
        {
            _tickSystem?.Stop();
        }

        private void HandleTick(uint tick, float deltaTime)
        {
            DispatchTick(tick, deltaTime);
            Tick?.Invoke(tick, deltaTime);
        }

        /// <summary>
        /// 处理Tick逻辑。Replica 不收集、不 Step。
        /// </summary>
        private void DispatchTick(uint tick, float deltaTime)
        {
            if (_entityRegistry == null) return;

            _tickCommands.Clear();
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!entity.Character || !entity.Character.IsInitialized) continue;
                if (entity.Identity != null && entity.Identity.IsReplica) continue;
                _tickCommands[pair.Key] = entity.CollectCommand(tick);
            }

            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                if (!_tickCommands.TryGetValue(pair.Key, out EntityCommand command)) continue;
                pair.Value.Character.Step(tick, deltaTime, command);
            }
        }

        #endregion
        
        #region 模拟实体管理

        public bool Register(EntityObjectIdentity identity, EntityCharacter character)
        {
            return _entityRegistry.Register(identity, character);
        }

        public bool Unregister(uint entityId)
        {
            return _entityRegistry.Unregister(entityId);
        }

        public bool TryGet(uint entityId, out EntityObjectIdentity identity, out EntityCharacter character)
        {
            identity = null;
            character = null;
            
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity))
            {
                return false;
            }

            identity = entity.Identity;
            character = entity.Character;
            return true;
        }
        
        /// <summary>
        /// 将意图来源挂到已注册槽位；不在核内缓存提供器
        /// </summary>
        public bool SetInputSource(uint entityId, IInputStateProvider inputSource)
        {
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity))
            {
                return false;
            }

            entity.SetInputSource(inputSource);
            return true;
        }

        #endregion

        #region 快照处理

        public bool TryCapture(uint entityId, out EntityRollbackState state)
        {
            state = default;
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (!entity.Character.IsInitialized) return false;
            
            state = entity.Character.CaptureRollbackState();
            return true;
        }

        /// <summary>
        /// 将回滚状态写回已注册且已初始化的实体；不解释网络序号。
        /// </summary>
        public bool TryRestore(uint entityId, in EntityRollbackState state)
        {
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (!entity.Character || !entity.Character.IsInitialized) return false;

            entity.Character.RestoreRollbackState(state);
            return true;
        }

        #endregion

        #region 生命周期

        public void Init()
        {
            _configHandle = Global.Load<SimulationConfig>("Config_SimulationConfig");
            SimulationConfig config = _configHandle.AssetObject as SimulationConfig;
                
            _entityRegistry = new EntityRegistry();
            _tickSystem = new TickSystem(config.simulationTickRate, config.maxSimulationTicksPerFrame);
            _tickSystem.Tick += HandleTick;
        }

        public void Update(float deltaTime)
        {
            if(IsRunning) _tickSystem.Update(deltaTime);
        }

        public void Destroy()
        {
            _configHandle.Dispose();
            
            if (_tickSystem != null)
            {
                _tickSystem.Tick -= HandleTick;
                _tickSystem.Stop();
                _tickSystem = null;
            }

            Tick = null;

            _tickCommands.Clear();
            _entityRegistry?.Clear();
        }

        #endregion
    }
}
