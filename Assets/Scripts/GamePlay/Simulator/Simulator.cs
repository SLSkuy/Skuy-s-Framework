using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 模拟核：持有唯一 Tick、实体注册表与步进调度。由 Host 子系统持有并驱动。
    /// </summary>
    public sealed class Simulator
    {
        private TickSystem _tickSystem;
        private EntityRegistry _entityRegistry;
        private readonly Dictionary<uint, EntityCommand> _tickCommands = new();

        #region 属性
        public uint CurrentTick => _tickSystem?.CurrentTick ?? 0;
        public bool IsRunning => _tickSystem is { IsRunning: true };
        public int RegisteredEntityCount => _entityRegistry?.Count ?? 0;
        #endregion

        public void Init()
        {
            SimulationConfig config = SimulationConfig.Instance;
            _entityRegistry = new EntityRegistry();
            _tickSystem = new TickSystem(config.simulationTickRate, config.maxSimulationTicksPerFrame);
            
            _tickSystem.Tick += DispatchTick;
        }

        public void Update(float deltaTime)
        {
            if(IsRunning) _tickSystem.Update(deltaTime);
        }

        public void Destroy()
        {
            if (_tickSystem != null)
            {
                _tickSystem.Tick -= DispatchTick;
                _tickSystem.Stop();
                _tickSystem = null;
            }

            _tickCommands.Clear();
            _entityRegistry?.Clear();
        }

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

        public bool TryCapture(uint entityId, out EntityRollbackState state)
        {
            state = default;
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (!entity.Character.IsInitialized) return false;
            
            state = entity.Character.CaptureRollbackState();
            return true;
        }

        public void StartClock()
        {
            if (IsRunning) return;
            _tickSystem.Start();
        }

        public void StopClock()
        {
            _tickSystem?.Stop();
        }

        /// <summary>
        /// 处理Tick逻辑
        /// </summary>
        private void DispatchTick(uint tick, float deltaTime)
        {
            if (_entityRegistry == null) return;

            // 捕获当前Tick实体意图
            _tickCommands.Clear();
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!entity.Character || !entity.Character.IsInitialized) continue;
                _tickCommands[pair.Key] = entity.CollectCommand(tick);
            }

            // 处理捕获的所有实体意图
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                if (!_tickCommands.TryGetValue(pair.Key, out EntityCommand command)) continue;
                pair.Value.Character.Step(tick, deltaTime, command);
            }
        }
    }
}
