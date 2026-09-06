using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 子模块管理类，负责所有模块的生命周期管理
    /// </summary>
    public class SystemManager : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.SystemManager;
        private readonly List<ISubSystem> _subSystems = new();
        private readonly List<ISubSystem> _systems2Remove = new();
        private bool _isTicking;
        private bool _needsSort;

        /// <summary>
        /// 注册并实例化子系统。返回时已完成 <see cref="ISubSystem._Init"/>，可立刻调用内部逻辑。
        /// 若注册发生在当前 Tick 遍历中，新系统从下一轮循环开始参与 Update。
        /// </summary>
        /// <typeparam name="T">子系统类型</typeparam>
        public T RegisterSystem<T>() where T : class, ISubSystem, new()
        {
            var system = new T();
            RegisterSystem(system);
            return system;
        }

        /// <summary>
        /// 注册并管理子系统。返回时已完成初始化。
        /// </summary>
        public void RegisterSystem(ISubSystem system)
        {
            if (system == null) return;
            if (_subSystems.Contains(system)) return;

            if (!system.IsInitialized)
            {
                system._Init();
            }

            _subSystems.Add(system);
            if (_isTicking)
            {
                _needsSort = true;
            }
            else
            {
                SortSystems();
            }
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
        /// 注销子系统。当前 Tick 结束后再销毁，避免遍历中途抽走正在更新的系统。
        /// </summary>
        public void UnregisterSystem(ISubSystem system)
        {
            if (system == null) return;
            if (_systems2Remove.Contains(system)) return;

            _systems2Remove.Add(system);
            if (!_isTicking)
            {
                RemoveSystem();
            }
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
            _needsSort = false;
        }

        /// <summary>
        /// 清除注销的系统
        /// </summary>
        private void RemoveSystem()
        {
            while (_systems2Remove.Count > 0)
            {
                ISubSystem system = _systems2Remove[^1];
                _systems2Remove.RemoveAt(_systems2Remove.Count - 1);
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

            SortSystems();
        }

        /// <summary>
        /// 循环外延迟处理，避免破环循环结构
        /// </summary>
        private void EndTick()
        {
            _isTicking = false;
            RemoveSystem();
            if (_needsSort)
            {
                SortSystems();
            }
        }

        #region 生命周期

        public override void Update(float deltaTime)
        {
            if (!IsInitialized) return;

            _isTicking = true;
            int count = _subSystems.Count;
            for (int i = 0; i < count; i++)
            {
                _subSystems[i].Update(deltaTime);
            }

            EndTick();
        }

        public override void LateUpdate()
        {
            if (!IsInitialized) return;

            _isTicking = true;
            int count = _subSystems.Count;
            for (int i = 0; i < count; i++)
            {
                _subSystems[i].LateUpdate();
            }
        }

        public override void FixedUpdate(float fixedDeltaTime)
        {
            if (!IsInitialized) return;

            _isTicking = true;
            int count = _subSystems.Count;
            for (int i = 0; i < count; i++)
            {
                _subSystems[i].FixedUpdate(fixedDeltaTime);
            }
        }

        public override void Destroy()
        {
            List<ISubSystem> snapshot = new(_subSystems);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                ISubSystem system = snapshot[i];
                if (system.IsInitialized)
                {
                    system._Destroy();
                }
            }
            _subSystems.Clear();
            _systems2Remove.Clear();
            _isTicking = false;
            _needsSort = false;
        }

        #endregion
    }
}
