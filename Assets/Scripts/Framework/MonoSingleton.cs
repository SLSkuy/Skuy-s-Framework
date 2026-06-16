using UnityEngine;

namespace Framework
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        
        /// <summary>
        /// 每个泛型对象都拥有一个独立的锁
        /// </summary>
        private static readonly object Lock = new();
        
        /// <summary>
        /// 每个泛型类型独立拥有，防止意外退出时调用
        /// </summary>
        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) + "' already destroyed on application quit.");
                    return null;
                }

                lock (Lock)
                {
                    if (!_instance)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (FindObjectsByType<T>(FindObjectsSortMode.InstanceID).Length > 1)
                        {
                            Debug.LogError("[Singleton] Multiple instances of " + typeof(T) + " found!");
                            return _instance;
                        }

                        if (!_instance)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(Singleton) " + typeof(T);

                            DontDestroyOnLoad(singleton);

                            Debug.Log("[Singleton] Created singleton instance of " + typeof(T));
                        }
                    }

                    return _instance;
                }
            }
        }

        protected void Awake()
        {
            _applicationIsQuitting = false;
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if(_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Init();
        }

        /// <summary>
        /// 单例初始化，由子类覆写
        /// </summary>
        protected virtual void Init()
        {
            
        }

        /// <summary>
        /// 程序退出销毁，需由全局调度器调用，避免出现顺序问题
        /// </summary>
        public void ShutDown()
        {
            if (!_instance || _instance != this) return;
            
            Destroy();
            
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// 单例销毁，由子类覆写
        /// </summary>
        protected virtual void Destroy()
        {
            
        }
    }
}