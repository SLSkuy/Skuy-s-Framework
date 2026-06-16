using System;
using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// 对象池协变接口，用于统一清空操作
    /// </summary>
    internal interface IObjectPool
    {
        void Clear();
    }

    /// <summary>
    /// 非 Mono 泛型对象池
    /// <para>适用于普通 C# 类对象复用</para>
    /// <para>通过 Func 创建对象，通过 Action 控制取出/回收逻辑</para>
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> : IObjectPool where T : class
    {
        /// <summary>
        /// 对象队列
        /// </summary>
        private readonly Queue<T> _pool = new();

        /// <summary>
        /// 创建对象方法
        /// </summary>
        private readonly Func<T> _createFunc;

        /// <summary>
        /// 取出对象时回调
        /// </summary>
        private readonly Action<T> _onGet;

        /// <summary>
        /// 回收对象时回调
        /// </summary>
        private readonly Action<T> _onRelease;

        /// <summary>
        /// 销毁对象时回调
        /// </summary>
        private readonly Action<T> _onDestroy;

        /// <summary>
        /// 最大缓存数量
        /// </summary>
        private readonly int _maxCount;
        
        public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, 
            Action<T> onDestroy = null, int defaultCapacity = 0, int maxCount = 32)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxCount = maxCount;

            // 预创建对象
            for (int i = 0; i < defaultCapacity; i++)
            {
                _pool.Enqueue(_createFunc());
            }
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = _createFunc();
            }

            _onGet?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null)
            {
                return;
            }

            // 超出最大容量直接销毁
            if (_pool.Count >= _maxCount)
            {
                _onDestroy?.Invoke(obj);
                return;
            }

            _onRelease?.Invoke(obj);

            _pool.Enqueue(obj);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                _onDestroy?.Invoke(obj);
            }
        }
    }
}