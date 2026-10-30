using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 管理实体能力模块，并保存配置、输入与共享模拟状态。
    /// </summary>
    public class EntityContext
    {
        private readonly Dictionary<Type, EntityModuleBase> _modules = new();
        private readonly EntityCharacter _character;

        public readonly CharacterConfig Config;
        public uint CurrentTick;
        
        #region 控制命令
        public Vector2 LastMoveInput;
        public Vector2 LastAimInput;
        #endregion

        #region 瞬时状态
        public bool RunToggleRequest;
        public bool JumpRequest;
        #endregion

        #region 持续状态
        public float LocomotionSpeed;

        public bool IsSprinting;
        public bool IsRunning;
        public bool IsFocus;
        #endregion

        public EntityContext(EntityCharacter character, CharacterConfig config)
        {
            _character = character;
            Config = config;
        }

        /// <summary>
        /// 按具体类型获取必需模块；未装配时直接暴露注册错误。
        /// 高频调用方应在初始化时缓存结果。
        /// </summary>
        public T Get<T>() where T : EntityModuleBase
        {
            return (T)_modules[typeof(T)];
        }

        /// <summary>
        /// 查询可能未装配的可选能力，不创建或初始化组件。
        /// </summary>
        public bool TryGet<T>(out T module) where T : EntityModuleBase
        {
            if (_modules.TryGetValue(typeof(T), out EntityModuleBase value))
            {
                module = (T)value;
                return true;
            }

            module = null;
            return false;
        }

        /// <summary>
        /// 收集并登记实体能力组件，此阶段不绑定或初始化组件。
        /// </summary>
        public T AddAbility<T>() where T : EntityModuleBase
        {
            T module = _character.GetOrAddComponent<T>();
            _modules.Add(typeof(T), module);
            return module;
        }

        /// <summary>
        /// 所有能力收集完成后统一绑定并初始化，模块不依赖绑定顺序。
        /// </summary>
        internal void BindAbilities()
        {
            foreach (EntityModuleBase module in _modules.Values)
            {
                module.Bind(_character);
            }
        }

        /// <summary>
        /// 在 Tick 末尾调用，清除瞬时边沿标志。
        /// 持续型输入（MoveInput/AimInput/IsSprintHeld/IsRunning）不清除。
        /// </summary>
        public void ResetTickFlags()
        {
            RunToggleRequest = false;
            JumpRequest = false;
        }
    }
}
