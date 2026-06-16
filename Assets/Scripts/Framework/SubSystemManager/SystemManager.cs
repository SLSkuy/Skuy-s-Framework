using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 子模块管理类，负责所有模块的生命周期管理
    /// </summary>
    public class SystemManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.SystemManager;
        private readonly List<ISubSystem> _subSystems = new();

        /// <summary>
        /// 注册并实例化管理子系统
        /// </summary>
        /// <typeparam name="T">子系统类型</typeparam>
        public T RegisterSystem<T>() where T : class, ISubSystem, new()
        {
            var system = new T();
            RegisterSystem(system);
            return system;
        }

        /// <summary>
        /// 注册并管理子系统
        /// </summary>
        public void RegisterSystem(ISubSystem system)
        {
            Type sysType = system.GetType();
            foreach (var s in _subSystems)
            {
                if (s.GetType() == sysType)
                {
                    Debug.LogWarning($"[SystemManager] {sysType.Name} already registered!");
                    return;
                }
            }
            
            if (_subSystems.Contains(system))
            {
                Debug.LogWarning($"[{GetType().Name}] System {system.GetType().Name} already registered!");
                return;
            }
            
            if (!system.IsInitialized)
            {
                system._Init();
            }
            
            _subSystems.Add(system);
            SortSystems();
        }

        /// <summary>
        /// 注销子系统
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterSystem<T>() where T : class, ISubSystem, new()
        {
            T system = GetSystem<T>();
            UnregisterSystem(system);
        }
        
        /// <summary>
        /// 注销子系统
        /// </summary>
        public void UnregisterSystem(ISubSystem system)
        {
            if (!_subSystems.Contains(system))
            {
                return;
            }

            if (system.IsInitialized)
            {
                system._Destroy();
            }

            _subSystems.Remove(system);
            SortSystems();
        }

        /// <summary>
        /// 获取子系统实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSystem<T>() where T : class, ISubSystem
        {
            foreach (ISubSystem system in _subSystems)
            {
                if (system is T target)
                {
                    return target;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据子组件优先级进行排序
        /// </summary>
        private void SortSystems()
        {
            _subSystems.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }

        #region 生命周期

        public override void Update(float deltaTime)
        {
            if (!IsInitialized) return;
            
            foreach (var system in _subSystems)
            {
                system.Update(deltaTime);
            }
        }

        public override void LateUpdate()
        {
            if (!IsInitialized) return;
            
            foreach (var system in _subSystems)
            {
                system.LateUpdate();
            }
        }

        public override void FixedUpdate(float fixedDeltaTime)
        {
            if (!IsInitialized) return;

            foreach (var system in _subSystems)
            {
                system.FixedUpdate(fixedDeltaTime);
            }
        }

        public override void Destroy()
        {
            for (int i = _subSystems.Count - 1; i >= 0; i--)
            {
                _subSystems[i]._Destroy();
            }
            _subSystems.Clear();
        }

        #endregion
    }
}
