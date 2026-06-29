using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 资源配置
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "Framework/ResourceConfig")]
    public sealed class ResourceConfig : ScriptableObject
    {
        public List<Entry> entries;

        [Serializable]
        public class Entry
        {
            /// <summary>
            /// 资源名称
            /// </summary>
            public string key;
            
            /// <summary>
            /// 是否为持久化数据
            /// </summary>
            public bool persistent;
        }
    }
}