using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 全局数据单例，提供对单例数据的快速访问方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance => Get();
        
        /// <summary>
        /// 全局数据单例获取
        /// </summary>
        public static T Get()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<T>(typeof(T).Name);

                if (!_instance)
                {
                    Debug.LogError($"Resources/{typeof(T).Name}.asset 不存在");
                }
            }

            return _instance;
        }
    }
}