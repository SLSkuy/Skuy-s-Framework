using GamePlay.EntitySystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 与传输无关的角色生成：加载原型、写入身份并初始化模拟。
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

        /// <summary>
        /// 根据原型创建角色。失败时销毁半成品且不抛出未捕获的场景残留。
        /// </summary>
        public static EntityObjectIdentity Spawn(GameObject prefab, Vector3 position, string objectName,
            uint entityId, EntityObjectRole role, uint ownerClientId)
        {
            if (!TrySpawn(prefab, position, objectName, entityId, role, ownerClientId, out EntityObjectIdentity identity,
                    out string error))
            {
                throw new System.InvalidOperationException(error);
            }

            return identity;
        }

        public static bool TrySpawn(GameObject prefab, Vector3 position, string objectName,
            uint entityId, EntityObjectRole role, uint ownerClientId, out EntityObjectIdentity identity)
        {
            return TrySpawn(prefab, position, objectName, entityId, role, ownerClientId, out identity, out _);
        }

        public static bool TrySpawn(GameObject prefab, Vector3 position, string objectName,
            uint entityId, EntityObjectRole role, uint ownerClientId, out EntityObjectIdentity identity, out string error)
        {
            identity = null;
            error = null;
            if (prefab == null)
            {
                error = "角色原型为空。";
                return false;
            }

            if (entityId == 0)
            {
                error = "entityId 不能为 0。";
                return false;
            }

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
            instance.name = objectName;
            identity = instance.GetComponent<EntityObjectIdentity>();
            EntityCharacter character = instance.GetComponent<EntityCharacter>();
            if (identity == null || character == null)
            {
                Object.Destroy(instance);
                identity = null;
                error = "原型缺少 EntityObjectIdentity 或 EntityCharacter。";
                return false;
            }

            identity.Init(entityId, ownerClientId, role);
            character.Init();
            MovementModule movement = identity.GetComponent<MovementModule>();
            movement?.SetReplicaMode(role == EntityObjectRole.Replica);
            return true;
        }
    }
}
