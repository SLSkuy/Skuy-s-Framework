using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 各 <see cref="ResGroup"/> 的池策略。资源放在 Resources/ResGroupPolicy.asset。
    /// </summary>
    [CreateAssetMenu(fileName = "ResGroupPolicy", menuName = "AppCore/ResGroupPolicy")]
    public class ResGroupPolicy : ScriptableObjectSingleton<ResGroupPolicy>
    {
        [Serializable]
        public struct Setting
        {
            public ResGroup group;
            public bool sceneClear;
            public bool idleUnload;
            public int maxIdleCount;
            public int prewarmCount;
            public float idleUnloadSeconds;
        }

        [SerializeField] private Setting[] settings =
        {
            new() { group = ResGroup.UI, sceneClear = false, idleUnload = true, maxIdleCount = 32, prewarmCount = 0, idleUnloadSeconds = 30f },
            new() { group = ResGroup.Audio, sceneClear = false, idleUnload = true, maxIdleCount = 32, prewarmCount = 0, idleUnloadSeconds = 30f },
            new() { group = ResGroup.VFX, sceneClear = true, idleUnload = true, maxIdleCount = 64, prewarmCount = 0, idleUnloadSeconds = 30f },
            new() { group = ResGroup.Prefab, sceneClear = true, idleUnload = true, maxIdleCount = 256, prewarmCount = 0, idleUnloadSeconds = 30f },
            new() { group = ResGroup.Temp, sceneClear = true, idleUnload = true, maxIdleCount = 32, prewarmCount = 0, idleUnloadSeconds = 30f }
        };

        private Dictionary<ResGroup, Setting> _lookup;

        /// <summary>
        /// 读取分组策略。资源缺失或该分组未配置时失败。
        /// </summary>
        public static Setting Get(ResGroup group)
        {
            ResGroupPolicy policy = Instance;
            if (!policy) throw new InvalidOperationException("Resources/ResGroupPolicy.asset 不存在");

            return policy.Find(group);
        }

        private Setting Find(ResGroup group)
        {
            if (_lookup == null) RebuildLookup();

            if (_lookup.TryGetValue(group, out Setting setting)) return setting;

            throw new InvalidOperationException($"ResGroup {group} 没有池策略");
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _lookup = new Dictionary<ResGroup, Setting>(settings.Length);
            for (int i = 0; i < settings.Length; i++) _lookup[settings[i].group] = settings[i];
        }
    }
}
