using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 模拟核子模块：持有唯一 Tick、实体注册表、命令邮箱与步进调度。由 Host 子系统持有并驱动。
    /// </summary>
    public sealed class Simulator
    {
        private TickSystem _tickSystem;
        private EntityRegistry _entityRegistry;
        private CommandMailbox _mailbox;
        private readonly Dictionary<uint, EntityCommandBuilder> _commandBuilders = new();
        private IInputStateProvider _deviceInput;
        private uint _possessedEntityId;

        #region 属性
        public uint CurrentTick => _tickSystem?.CurrentTick ?? 0;
        public bool IsRunning => _tickSystem != null && _tickSystem.IsRunning;
        public int RegisteredEntityCount => _entityRegistry?.Count ?? 0;
        public uint PossessedEntityId => _possessedEntityId;
        #endregion

        public void Init()
        {
            SimulationConfig config = SimulationConfig.Instance;
            _entityRegistry = new EntityRegistry();
            _mailbox = new CommandMailbox();
            _tickSystem = new TickSystem(config.simulationTickRate, config.maxSimulationTicksPerFrame);
            _tickSystem.Tick += DispatchTick;
        }

        public void Update(float deltaTime)
        {
            _tickSystem?.Update(deltaTime);
        }

        public void Destroy()
        {
            if (_tickSystem != null)
            {
                _tickSystem.Tick -= DispatchTick;
                _tickSystem.Stop();
                _tickSystem = null;
            }

            _commandBuilders.Clear();
            _mailbox?.Clear();
            _entityRegistry?.Clear();
            _possessedEntityId = 0;
            _deviceInput = null;
        }

        public void SetDeviceInput(IInputStateProvider inputProvider)
        {
            _deviceInput = inputProvider;
        }

        public bool Register(EntityObjectIdentity identity, EntityCharacter character)
        {
            if (_entityRegistry == null || !_entityRegistry.Register(identity, character)) return false;
            _commandBuilders[identity.EntityId] = new EntityCommandBuilder();
            return true;
        }

        public bool Unregister(uint entityId)
        {
            if (_entityRegistry == null || !_entityRegistry.Unregister(entityId)) return false;
            _commandBuilders.Remove(entityId);
            _mailbox?.RemoveEntity(entityId);
            if (_possessedEntityId == entityId) _possessedEntityId = 0;
            return true;
        }

        public bool TryGet(uint entityId, out EntityObjectIdentity identity, out EntityCharacter character)
        {
            identity = null;
            character = null;
            if (_entityRegistry == null || !_entityRegistry.TryGet(entityId, out RegisteredEntity entity))
            {
                return false;
            }

            identity = entity.Identity;
            character = entity.Character;
            return true;
        }

        public void SubmitInput(uint entityId, in InputState state)
        {
            _mailbox?.Submit(entityId, state);
        }

        public bool Possess(uint entityId)
        {
            if (_entityRegistry == null || !_entityRegistry.Contains(entityId)) return false;
            _possessedEntityId = entityId;
            return true;
        }

        public void Unpossess()
        {
            _possessedEntityId = 0;
        }

        public bool TryCapture(uint entityId, out EntityRollbackState state)
        {
            state = default;
            if (_entityRegistry == null || !_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (entity.Character == null || !entity.Character.IsInitialized) return false;
            state = entity.Character.CaptureRollbackState();
            return true;
        }

        public void StartClock()
        {
            if (_tickSystem == null || _tickSystem.IsRunning) return;
            _tickSystem.Start();
        }

        public void StopClock()
        {
            _tickSystem?.Stop();
        }

        private void DispatchTick(uint tick, float deltaTime)
        {
            if (_possessedEntityId != 0 && _deviceInput != null)
            {
                SubmitInput(_possessedEntityId, _deviceInput.GetInputState());
            }

            if (_entityRegistry == null) return;
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (entity.Character == null || !entity.Character.IsInitialized) continue;
                if (!_commandBuilders.TryGetValue(pair.Key, out EntityCommandBuilder builder)) continue;

                InputState input = _mailbox.Consume(pair.Key);
                entity.Character.Step(tick, deltaTime, builder.Build(tick, input));
            }
        }
    }
}
