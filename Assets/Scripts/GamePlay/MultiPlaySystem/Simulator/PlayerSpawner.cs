using GamePlay.Simulator;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 测试用角色生成：加载 NetPlayer 并写入网络身份。
    /// </summary>
    public static class PlayerSpawner
    {
        public const string PlayerPrefabPath = "NetPlayer";

        public static GameObject LoadPrefab()
        {
            GameObject prefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (prefab == null) throw new System.InvalidOperationException($"缺少 Resources Prefab：{PlayerPrefabPath}");
            return prefab;
        }

        public static EntityObjectIdentity Spawn(GameObject prefab, Vector3 position, string objectName,
            uint entityId, EntityObjectRole role, uint ownerClientId)
        {
            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            instance.name = objectName;
            EntityObjectIdentity identity = instance.GetComponent<EntityObjectIdentity>();
            if (identity == null)
            {
                throw new System.InvalidOperationException("NetPlayer Prefab 缺少 NetworkObjectIdentity。");
            }

            identity.Init(entityId, ownerClientId, role);
            return identity;
        }
    }
}
