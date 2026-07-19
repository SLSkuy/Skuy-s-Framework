using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络玩家实体管理器。主机和客户端模式互斥，因此每个进程只显示一套玩家实体。
    /// </summary>
    public class NetworkPlayerManager
    {
        private const string PlayerPrefabPath = "Prefabs/EntitySystem/NetPlayerCharacter";

        public int HostPlayerCount => _hostPlayers.Count;
        public int ClientPlayerCount => _clientPlayers.Count;

        private readonly Dictionary<uint, NetPlayerCharacter> _hostPlayers = new();
        private readonly Dictionary<uint, NetPlayerCharacter> _clientPlayers = new();
        private readonly Transform _hostRoot;
        private readonly Transform _clientRoot;

        public NetworkPlayerManager()
        {
            _hostRoot = CreateRoot("Network Players (Server)");
            _clientRoot = CreateRoot("Network Players (Client)");
        }

        public bool TryGetHostPlayer(uint clientId, out NetPlayerCharacter player)
        {
            return _hostPlayers.TryGetValue(clientId, out player);
        }

        public bool TryGetClientPlayer(uint clientId, out NetPlayerCharacter player)
        {
            return _clientPlayers.TryGetValue(clientId, out player);
        }

        public NetPlayerCharacter GetOrCreateHostPlayer(uint clientId, out bool created)
        {
            if (_hostPlayers.TryGetValue(clientId, out NetPlayerCharacter player))
            {
                created = false;
                return player;
            }

            player = CreatePlayer(clientId, _hostRoot, NetEntityRole.Authority);
            created = player != null;
            if (!created) return null;

            _hostPlayers.Add(clientId, player);
            return player;
        }

        public NetPlayerCharacter GetOrCreateClientPlayer(uint clientId, out bool created)
        {
            if (_clientPlayers.TryGetValue(clientId, out NetPlayerCharacter player))
            {
                created = false;
                return player;
            }

            player = CreatePlayer(clientId, _clientRoot, NetEntityRole.Replica);
            created = player != null;
            if (!created) return null;

            _clientPlayers.Add(clientId, player);
            return player;
        }

        public IInputStateProvider EnsureLocalInput(NetPlayerCharacter player)
        {
            if (!player) return null;

            LocalInputProvider provider = player.GetComponent<LocalInputProvider>();
            return provider ? provider : player.gameObject.AddComponent<LocalInputProvider>();
        }

        public bool RemoveHostPlayer(uint clientId)
        {
            if (!_hostPlayers.Remove(clientId, out NetPlayerCharacter player)) return false;
            DestroyObject(player.gameObject);
            return true;
        }

        public bool RemoveClientPlayer(uint clientId)
        {
            if (!_clientPlayers.Remove(clientId, out NetPlayerCharacter player)) return false;
            DestroyObject(player.gameObject);
            return true;
        }

        public void RemoveClientPlayersExcept(ISet<uint> activeClientIds)
        {
            List<uint> removedIds = null;
            foreach (uint clientId in _clientPlayers.Keys)
            {
                if (activeClientIds.Contains(clientId)) continue;
                removedIds ??= new List<uint>();
                removedIds.Add(clientId);
            }

            if (removedIds == null) return;
            foreach (uint clientId in removedIds)
            {
                RemoveClientPlayer(clientId);
            }
        }

        public void ClearHostPlayers()
        {
            Clear(_hostPlayers);
        }

        public void ClearClientPlayers()
        {
            Clear(_clientPlayers);
        }

        public void Dispose()
        {
            ClearHostPlayers();
            ClearClientPlayers();
            if (_hostRoot) DestroyObject(_hostRoot.gameObject);
            if (_clientRoot) DestroyObject(_clientRoot.gameObject);
        }

        private static NetPlayerCharacter CreatePlayer(uint clientId, Transform parent, NetEntityRole role)
        {
            Vector3 position = GetSpawnPosition(clientId);
            GameObject instance = Global.Instantiate(PlayerPrefabPath, position, Quaternion.identity, parent);
            if (!instance)
            {
                GameObject prefab = Resources.Load<GameObject>(PlayerPrefabPath);
                if (prefab) instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            }

            if (!instance)
            {
                Debug.LogError($"[NetworkPlayerManager] Player prefab not found: {PlayerPrefabPath}");
                return null;
            }

            instance.name = $"Network Player {clientId}";
            NetPlayerCharacter player = instance.GetComponent<NetPlayerCharacter>();
            if (!player)
            {
                Debug.LogError("[NetworkPlayerManager] Player prefab has no NetPlayerCharacter component.");
                DestroyObject(instance);
                return null;
            }

            player.Index = clientId;
            player.SetRole(role);
            return player;
        }

        private static Vector3 GetSpawnPosition(uint clientId)
        {
            int index = Mathf.Max(0, (int)clientId - 1);
            return new Vector3(index % 4 * 2.5f, 1f, index / 4f * 2.5f);
        }

        private static Transform CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        private static void Clear(Dictionary<uint, NetPlayerCharacter> players)
        {
            foreach (NetPlayerCharacter player in players.Values)
            {
                if (player) DestroyObject(player.gameObject);
            }
            players.Clear();
        }

        private static void DestroyObject(Object target)
        {
            if (!target) return;
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}
