using Framework;
using UnityEngine;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 临时预制体索引缓冲，后续替换为资源加载器加载
    /// </summary>
    [CreateAssetMenu(fileName = "EntityPrefabConfig", menuName = "GamePlay/EntityPrefabConfig", order = 0)]
    public class EntityPrefabConfig : ScriptableObjectSingleton<EntityPrefabConfig>
    {
        [Header("玩家预制体")]
        public GameObject playerPrefab;
    }
}