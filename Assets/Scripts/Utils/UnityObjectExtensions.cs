using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Unity 对象扩展工具。
    /// </summary>
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// 尝试获取指定组件，若不存在则自动添加。
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject obj)
            where T : Component
        {
            if (obj.TryGetComponent(out T component))
            {
                return component;
            }

            return obj.AddComponent<T>();
        }

        /// <summary>
        /// 尝试获取指定组件，若不存在则自动添加。
        /// </summary>
        public static T GetOrAddComponent<T>(this Component obj)
            where T : Component
        {
            if (obj.TryGetComponent(out T component))
            {
                return component;
            }

            return obj.gameObject.AddComponent<T>();
        }
    }
}