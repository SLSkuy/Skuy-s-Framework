using System;
using System.Collections.Generic;
using Framework;
using GamePlay.NetSync;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 单机实体共享的全局固定 Tick 驱动系统。
    /// </summary>
    public sealed class EntitySimulationSystem : SubSystemBase
    {
        private sealed class LocalEntry
        {
            public IEntitySimulation Simulation;
            public Func<uint, EntityInputCommand> CommandSource;
        }

        private readonly Dictionary<object, LocalEntry> _localEntries = new();
        private NetworkTimeSystem _networkTime;

        #region 属性
        public override SubSystemPriority Priority => SubSystemPriority.EntitySimulationSystem;
        public uint CurrentTick => _networkTime?.CurrentTick ?? 0;
        #endregion

        /// <summary>
        /// 注册一个单机模拟目标及其命令来源。
        /// </summary>
        public void RegisterLocal(object owner, IEntitySimulation simulation, Func<uint, EntityInputCommand> commandSource)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            if (commandSource == null) throw new ArgumentNullException(nameof(commandSource));

            _localEntries[owner] = new LocalEntry
            {
                Simulation = simulation,
                CommandSource = commandSource
            };
        }

        /// <summary>
        /// 注销单机模拟目标。
        /// </summary>
        public void UnregisterLocal(object owner)
        {
            if (owner != null) _localEntries.Remove(owner);
        }

        public override void Init()
        {
            _networkTime = Global.Get<NetworkTimeSystem>();
            if (_networkTime != null) _networkTime.Tick += SimulateTick;
        }

        public override void Update(float deltaTime)
        {
        }

        public override void Destroy()
        {
            if (_networkTime != null) _networkTime.Tick -= SimulateTick;

            _localEntries.Clear();
        }

        private void SimulateTick(uint tick, float deltaTime)
        {
            foreach (LocalEntry entry in _localEntries.Values)
            {
                EntityInputCommand command = entry.CommandSource(tick);
                entry.Simulation.Step(tick, deltaTime, command);
            }
        }
    }
}
