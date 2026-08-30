using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 子模块管理类，负责所有模块的生命周期管理
    /// </summary>
    public class SystemManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.SystemManager;
        private readonly List<ISubSystem> _subSystems = new();
        private readonly List<ISubSystem> _systems2Add = new();
        private readonly List<ISubSystem> _systems2Remove = new();

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
            _systems2Add.Add(system);
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
            _systems2Remove.Add(system);
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

        /// <summary>
        /// 并入待注册的系统
        /// </summary>
        private void AddSystem()
        {
            if (_systems2Add.Count <= 0) return;

            foreach (var system in _systems2Add)
            {
                if (_subSystems.Contains(system))
                {
                    continue;
                }

                if (!system.IsInitialized)
                {
                    system._Init();
                }

                _subSystems.Add(system);
            }
            _systems2Add.Clear();
            SortSystems();
        }

        /// <summary>
        /// 清除注销的系统
        /// </summary>
        private void RemoveSystem()
        {
            if (_systems2Remove.Count <= 0) return;

            foreach (var system in _systems2Remove)
            {
                if (!_subSystems.Contains(system))
                {
                    continue;
                }

                if (system.IsInitialized)
                {
                    system._Destroy();
                }

                _subSystems.Remove(system);
            }
            _systems2Remove.Clear();
            SortSystems();
        }

        #region 生命周期

        public override void Update(float deltaTime)
        {
            if (!IsInitialized) return;

            foreach (var system in _subSystems)
            {
                system.Update(deltaTime);
            }

            AddSystem();
            RemoveSystem();
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
            _systems2Add.Clear();
            _systems2Remove.Clear();
        }

        #endregion
    }
}
