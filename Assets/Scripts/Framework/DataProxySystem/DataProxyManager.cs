using System;
using System.Collections.Generic;

namespace Framework
{
    public class DataProxyManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.DataProxyManager;

        private readonly Dictionary<Type, IDataProxy> _dataProxyDict = new();

        /// <summary>
        /// 注册数据代理类，无需传入实例化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T RegisterDataProxy<T>() where T : class, IDataProxy, new()
        {
            Type type = typeof(T);
            var proxy = new T();
            proxy._Init();
            _dataProxyDict.TryAdd(type, proxy);
            return proxy;
        }

        /// <summary>
        /// 注册数据代理类，需要传入已经实例化后的对象
        /// </summary>
        /// <param name="dataProxy"></param>
        public void RegisterDataProxy(IDataProxy dataProxy)
        {
            if (dataProxy == null) return;
            if (!dataProxy.IsInitialized) dataProxy._Init();
            _dataProxyDict.TryAdd(dataProxy.GetType(), dataProxy);
        }

        /// <summary>
        /// 删除数据代理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterDataProxy<T>() where T : class, IDataProxy
        {
            if (!_dataProxyDict.TryGetValue(typeof(T), out var proxy)) return;
            proxy._Clear();
            _dataProxyDict.Remove(typeof(T));
        }

        /// <summary>
        /// 获取数据代理
        /// </summary>
        /// <typeparam name="T">数据代理类型</typeparam>
        public T GetDataProxy<T>() where T : class, IDataProxy
        {
            if (_dataProxyDict.TryGetValue(typeof(T), out IDataProxy proxy))
            {
                return proxy as T;
            }
            
            return null;
        }
        
        /// <summary>
        /// 尝试获取数据代理，更推荐使用该方法
        /// </summary>
        public bool TryGetDataProxy<T>(out T proxy) where T : class, IDataProxy
        {
            if (_dataProxyDict.TryGetValue(typeof(T), out IDataProxy dataProxy))
            {
                proxy = dataProxy as T;
                return true;
            }

            proxy = null;
            return false;
        }

        #region 生命周期
        
        public override void Destroy()
        {
            foreach (IDataProxy proxy in _dataProxyDict.Values)
            {
                proxy._Clear();
            }

            _dataProxyDict.Clear();
        }

        #endregion
    }
}