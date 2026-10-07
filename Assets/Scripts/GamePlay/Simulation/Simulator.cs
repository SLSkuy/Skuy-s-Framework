using System;
using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;
using YooAsset;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟核：持有唯一 Tick、实体注册表与步进调度。每拍先处理玩家移动输入，再处理业务逻辑。
    /// </summary>
    public sealed class Simulator
    {
        private readonly Dictionary<uint, EntityCommand> _tickCommands = new();
        private EntityRegistry _entityRegistry;
        private TickSystem _snapshotTickSystem;
        private TickSystem _tickSystem;
        
        private AssetHandle _configHandle;
        
        private int _maxBufferedInputs;
        private int _maxFutureInputTicks;
        private int _snapshotTickRate;
        private int _interpolationDelayTicks;
        private float _positionSnapThreshold;
        private float _rotationSnapThresholdDegrees;

        #region 属性
        public uint CurrentTick => _tickSystem?.CurrentTick ?? 0;
        public bool IsRunning => _tickSystem is { IsRunning: true };
        public int MaxBufferedInputs => _maxBufferedInputs;
        public int MaxFutureInputTicks => _maxFutureInputTicks;
        public int SnapshotTickRate => _snapshotTickRate;
        public int InterpolationDelayTicks => _interpolationDelayTicks;
        public float PositionSnapThreshold => _positionSnapThreshold;
        public float RotationSnapThresholdDegrees => _rotationSnapThresholdDegrees;
        #endregion

        #region 事件
        /// <summary>
        /// 本拍实体移动之前。订阅者在此提交玩家移动输入。
        /// </summary>
        public event Action<uint, float> TickPlayerInput;

        /// <summary>
        /// 本拍移动步进完成之后、权威状态采样之前。订阅者在此推进业务逻辑。
        /// </summary>
        public event Action<uint, float> TickBusiness;

        /// <summary>
        /// 本拍权威状态采样之后。
        /// </summary>
        public event Action<uint, float> TickCaptured;

        /// <summary>
        /// 快照节拍。与模拟节拍分开累计，开了快照时钟后才触发。
        /// </summary>
        public event Action<uint, float> TickSnapshot;
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

        public void StartSnapshotClock()
        {
            if (_snapshotTickSystem.IsRunning) return;
            _snapshotTickSystem.Start();
        }

        public void StopSnapshotClock()
        {
            _snapshotTickSystem?.Stop();
        }

        private void HandleTick(uint tick, float deltaTime)
        {
            PrepareSimulationTick();
            TickPlayerInput?.Invoke(tick, deltaTime);
            DispatchTick(tick, deltaTime);
            TickBusiness?.Invoke(tick, deltaTime);
            CaptureAuthorityTick();
            TickCaptured?.Invoke(tick, deltaTime);
        }

        private void HandleSnapshotTick(uint tick, float deltaTime)
        {
            TickSnapshot?.Invoke(tick, deltaTime);
        }

        /// <summary>
        /// 处理Tick逻辑，进行模拟步进
        /// Replica 不收集、不 Step
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

        #region 插值处理

        /// <summary>
        /// 获取模拟步进前的Tick状态
        /// </summary>
        private void PrepareSimulationTick()
        {
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.PrepareSimulationTick();
            }
        }

        /// <summary>
        /// 获取模拟步进后的Tick状态
        /// </summary>
        private void CaptureAuthorityTick()
        {
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.CaptureAuthorityAfterTick();
            }
        }

        /// <summary>
        /// 插值步进流程
        /// </summary>
        private void PresentVisualPoses()
        {
            float alpha = _tickSystem.InterpolationAlpha;
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!ShouldPresent(entity)) continue;
                entity.Character.PresentVisualPose(alpha);
            }
        }

        /// <summary>
        /// 判断实体是否应该进行插值
        /// 只有主机端上的玩家和客户端上的主控玩家需要进行步进插值
        /// </summary>
        private static bool ShouldPresent(RegisteredEntity entity)
        {
            if (!entity.Character || !entity.Character.IsInitialized) return false;
            if (entity.Identity && entity.Identity.IsReplica) return false;
            return true;
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

        public bool TryCaptureEntityState(uint entityId, out EntitySnapshot state)
        {
            state = default;
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (!entity.Character.IsInitialized) return false;
            
            state = entity.Character.CaptureSnapshot();
            return true;
        }

        /// <summary>
        /// 将回滚状态写回已注册且已初始化的实体；不解释网络序号。
        /// </summary>
        public bool TryRestoreEntityState(uint entityId, in EntitySnapshot state)
        {
            if (!_entityRegistry.TryGet(entityId, out RegisteredEntity entity)) return false;
            if (!entity.Character || !entity.Character.IsInitialized) return false;

            entity.Character.RestoreSnapshot(state);
            return true;
        }

        /// <summary>
        /// 采样当前已初始化实体的回滚状态。
        /// </summary>
        public void CaptureEntities(List<PlayerProcessedSnapshot> samples)
        {
            samples.Clear();
            foreach (KeyValuePair<uint, RegisteredEntity> pair in _entityRegistry.Entities)
            {
                RegisteredEntity entity = pair.Value;
                if (!entity.Character || !entity.Character.IsInitialized) continue;

                samples.Add(new PlayerProcessedSnapshot
                {
                    entityId = pair.Key,
                    playerId = entity.Identity.PlayerId,
                    state = entity.Character.CaptureSnapshot(),
                });
            }
        }

        #endregion

        #region 生命周期

        public void Init()
        {
            _configHandle = Global.Load<SimulationConfig>("Config_SimulationConfig");
            SimulationConfig config = _configHandle.AssetObject as SimulationConfig;
            _maxBufferedInputs = config.maxBufferedInputs;
            _maxFutureInputTicks = config.maxFutureInputTicks;
            _snapshotTickRate = config.snapshotTickRate;
            _interpolationDelayTicks = config.interpolationDelayTicks;
            _positionSnapThreshold = config.positionSnapThreshold;
            _rotationSnapThresholdDegrees = config.rotationSnapThresholdDegrees;
            
            _entityRegistry = new EntityRegistry();
            
            _tickSystem = new TickSystem(config.simulationTickRate, config.maxSimulationTicksPerFrame);
            _tickSystem.Tick += HandleTick;
            
            _snapshotTickSystem = new TickSystem(config.snapshotTickRate, config.maxSimulationTicksPerFrame);
            _snapshotTickSystem.Tick += HandleSnapshotTick;
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            _tickSystem.Update(deltaTime);
            PresentVisualPoses();
            _snapshotTickSystem.Update(deltaTime);
        }

        public void Destroy()
        {
            _configHandle?.Dispose();
            _configHandle = null;
            
            if (_tickSystem != null)
            {
                _tickSystem.Tick -= HandleTick;
                _tickSystem.Stop();
                _tickSystem = null;
            }

            if (_snapshotTickSystem != null)
            {
                _snapshotTickSystem.Tick -= HandleSnapshotTick;
                _snapshotTickSystem.Stop();
                _snapshotTickSystem = null;
            }
            
            _entityRegistry?.Clear();
            _tickCommands.Clear();
            TickPlayerInput = null;
            TickBusiness = null;
            TickCaptured = null;
            TickSnapshot = null;
        }

        #endregion
    }
}
