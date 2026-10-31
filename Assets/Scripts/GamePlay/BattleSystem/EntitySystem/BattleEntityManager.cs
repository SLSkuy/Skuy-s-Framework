using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using NetSync;
using Utils;
using GamePlay.DataProxy;
using UnityEngine;
using YooAsset;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 战局内实体管理器，负责实体的生成/销毁
    /// </summary>
    public class BattleEntityManager : IDisposable
    {
        private readonly EntitySpawnConfigProxy _spawnConfigData = Global.GetDataProxy<EntitySpawnConfigProxy>();
        private readonly PlayerInfoDataProxy _playerData = Global.GetDataProxy<PlayerInfoDataProxy>();
        private readonly GameSaveDataProxy _saveData = Global.GetDataProxy<GameSaveDataProxy>();
        
        private readonly Dictionary<uint, EntityIdentity> _entities = new();
        private readonly Dictionary<Guid, PlayerIdentity> _players = new();
        private uint _nextEntityId = 1; // 实体ID从1开始分配
        
        private readonly BattleEntityServerHandler _serverHandler;
        private readonly BattleEntityClientHandler _clientHandler;
        
        private readonly ISimulationKernel _simulator;

        public BattleEntityManager(ISimulationKernel kernel)
        {
            _simulator = kernel;
            if (_simulator is HostSimulationKernel)
            {
                _serverHandler = Global.RegisterHandler<BattleEntityServerHandler>();
                _serverHandler.SpawnSyncRequested += HandleSpawnSyncRequested;
            }
            else
            {
                _clientHandler = Global.RegisterHandler<BattleEntityClientHandler>();
                _clientHandler.PlayerSpawnReceived += HandlePlayerSpawnReceived;
                _clientHandler.EntityDestroyReceived += HandleEntityDestroyReceived;
            }
        }

        /// <summary>
        /// 请求获取所有实体数据
        /// </summary>
        public void RequestAllEntities()
        {
            _clientHandler.SendSpawnSyncRequest();
        }

        #region 实体生成管理

        /// <summary>
        /// 同步生成本地实体对象
        /// </summary>
        public GameObject SpawnLocalEntity(in EntitySpawnCommand command)
        {
            GameObject instance = InstantiateEntity(command);
            if (instance.TryGetComponent<EntityIdentity>(out _))
            {
                throw new InvalidOperationException($"实例 {instance.name} 存在 {nameof(EntityIdentity)}，需使用SpawnNetEntity函数进行网络实体生成");
            }
            return instance;
        }

        /// <summary>
        /// 生成远端实体对象
        /// </summary>
        public EntityIdentity SpawnNetEntity(in EntitySpawnCommand command)
        {
            GameObject instance = InstantiateEntity(command);
            if (!instance.TryGetComponent(out EntityIdentity entityIdentity))
            {
                throw new InvalidOperationException($"实例 {instance.name} 缺少 {nameof(EntityIdentity)}，不能分配实体号");
            }
            
            AllocateId(entityIdentity);
            entityIdentity.BindEntityType(command.entityType);
            return entityIdentity;
        }

        private GameObject InstantiateEntity(in EntitySpawnCommand command)
        {
            string location = _spawnConfigData.GetEntityLocation(command.entityType);
            InstantiateOptions options = new (true, command.position, command.rotation);
            GameObject instance = Global.Instantiate(location, options);
            return instance;
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void DestroyEntity(in EntityDestroyCommand command)
        {
            if (RemoveEntity(command.entityId, out EntityIdentity identity))
            {
                if (identity is PlayerIdentity playerIdentity) _players.Remove(playerIdentity.PlayerID);
                Global.Release(identity.gameObject);   
            }
        }

        public void DestroyAll()
        {
            foreach (EntityIdentity entityIdentity in _entities.Values)
            {
                Global.Release(entityIdentity.gameObject);
            }
        }

        #endregion

        #region 玩家管理

        public void OnPlayerJoin(Guid playerId)
        {
            EntitySnapshot snapshot = _saveData.GetPlayerSnapshot(playerId);
            EntitySpawnCommand command = new EntitySpawnCommand
            {
                entityType = EntityType.Player,
                position = snapshot.movement.rootPosition,
                rotation = Quaternion.identity,
            };
            
            // 主机装配远端玩家
            EntityIdentity identity = SpawnNetEntity(command);
            bool isLocal = playerId == _playerData.UUID;
            AssemblePlayer(identity, playerId, snapshot, EntityRole.Authority, isLocal);
            
            _serverHandler.BroadcastPlayerSpawnNotify(identity.EntityId, playerId, command, snapshot);
        }

        /// <summary>
        /// 服务端处理玩家退出
        /// </summary>
        public void OnPlayerLeave(Guid playerId)
        {
            PlayerIdentity playerIdentity = _players[playerId];
            RestorePlayer(playerId);
            
            _simulator.Kernel.Unregister(playerIdentity.EntityId);
            ((HostSimulationKernel)_simulator).UnregisterRemoteInput(playerId);
            _players.Remove(playerId);

            EntityDestroyCommand command = new EntityDestroyCommand { entityId = playerIdentity.EntityId };
            DestroyEntity(command);
            _serverHandler.BroadcastEntityDestroyNotify(command);
        }

        /// <summary>
        /// 装配玩家实体状态，绑定玩家ID/模拟角色
        /// </summary>
        private void AssemblePlayer(EntityIdentity identity, Guid playerId, in EntitySnapshot snapshot, EntityRole role, bool isLocal)
        {
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            character.Init(role == EntityRole.Replica);
            character.RestoreSnapshot(snapshot);

            PlayerIdentity playerIdentity = (PlayerIdentity)identity;
            playerIdentity.BindPlayerID(playerId);
            playerIdentity.BindEntityRole(role);
                
            _players.Add(playerId, playerIdentity);
            _simulator.Kernel.Register(playerIdentity, character);

            // 本地操控玩家，设置摄像机跟随
            if (isLocal)
            {
                Global.Get<CameraManager>().SetTarget(character.Context.Get<ViewModule>().Orientation);
                _simulator.Kernel.SetInputSource(identity.EntityId, Global.Get<LocalInputManager>().Provider);
            }
            else
            {
                // 主机端装配远程输入
                if(_simulator is HostSimulationKernel kernel) kernel.RegisterRemoteInput(playerId, identity.EntityId); 
            }
        }

        #endregion

        #region 查询

        public bool TryGetEntity(uint entityId, out EntityIdentity entityIdentity)
        {
            return _entities.TryGetValue(entityId, out entityIdentity);
        }

        public bool TryGetPlayer(Guid playerId, out PlayerIdentity playerIdentity)
        {
            return _players.TryGetValue(playerId, out playerIdentity);
        }

        public List<EntityIdentity> GetActiveEntities()
        {
            return _entities.Values.ToList();
        }

        #endregion

        #region 实体ID

        private uint AllocateId(EntityIdentity identity)
        {
            identity.BindEntityID(_nextEntityId++);
            _entities.Add(identity.EntityId, identity);
            return identity.EntityId;
        }

        private void AddEntity(uint entityId, EntityIdentity identity)
        {
            identity.BindEntityID(entityId);
            _entities.Add(entityId, identity);
        }

        private bool RemoveEntity(uint entityId, out EntityIdentity identity)
        {
            if (!_entities.Remove(entityId, out identity)) return false;
            if (identity is PlayerIdentity playerIdentity) _players.Remove(playerIdentity.PlayerID);
            return true;
        }

        private bool RemoveEntity(uint entityId)
        {
            if(!_entities.Remove(entityId, out EntityIdentity identity)) return false;
            if(identity is PlayerIdentity playerIdentity) _players.Remove(playerIdentity.PlayerID);
            return true;
        }

        #endregion

        #region 存档管理

        public void RestoreEntity(uint entityId)
        {
            EntityIdentity identity = _entities[entityId];
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            // TODO: 存档实体数据
        }

        /// <summary>
        /// 存档单个玩家实体数据
        /// </summary>
        public void RestorePlayer(Guid playerId)
        {
            EntityIdentity identity = _players[playerId];
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            _saveData.RestorePlayerSnapshot(playerId, character.CaptureSnapshot());
        }

        /// <summary>
        /// 存储当前所有实体的快照到存档中
        /// </summary>
        public void RestoreAllEntity()
        {
            // 存储玩家实体
            foreach (PlayerIdentity identity in _players.Values)
            {
                RestorePlayer(identity.PlayerID);
            }
        }

        #endregion

        #region 生命周期

        public void Dispose()
        {
            DestroyAll();
            
            if (_clientHandler != null)
            {
                _clientHandler.PlayerSpawnReceived -= HandlePlayerSpawnReceived;
                _clientHandler.EntityDestroyReceived -= HandleEntityDestroyReceived;
                Global.UnregisterHandler<BattleEntityClientHandler>();
            }
            if (_serverHandler != null)
            {
                _serverHandler.SpawnSyncRequested -= HandleSpawnSyncRequested;
                Global.UnregisterHandler<BattleEntityServerHandler>();
            }
            
            _entities.Clear();
            _players.Clear();
            
            _nextEntityId = 1;
        }

        #endregion

        #region 客户端消息处理

        private void HandlePlayerSpawnReceived(Player_Spawn_Notify notify)
        {
            uint entityId = notify.EntityId;
            Guid playerId = Guid.Parse(notify.PlayerId);
            EntitySpawnCommand command = notify.ToCommand();
            EntitySnapshot snapshot = notify.Snapshot.ToSnapshot();
            GameObject instance = InstantiateEntity(command);
            if (!instance.TryGetComponent(out EntityIdentity identity))
            {
                throw new InvalidOperationException($"实例 {instance.name} 缺少 {nameof(EntityIdentity)}，不能分配实体号");
            }
            
            AddEntity(entityId, identity);
            identity.BindEntityType(command.entityType);
            
            bool isLocal = playerId == _playerData.UUID;    // 判断是否为本地玩家
            EntityRole role = isLocal ? EntityRole.Predict : EntityRole.Replica;
            AssemblePlayer(identity, playerId, snapshot, role, isLocal);
        }
        
        private void HandleEntityDestroyReceived(Entity_Destroy_Notify notify)
        {
            EntityDestroyCommand command = notify.ToCommand();
            DestroyEntity(command);
            _simulator.Kernel.Unregister(command.entityId);
        }

        #endregion
        
        #region 服务端消息处理

        /// <summary>
        /// 处理实体全量同步请求
        /// </summary>
        private void HandleSpawnSyncRequested(Guid playerId)
        {
            // 遍历所有玩家
            foreach (var pair in _players)
            {
                PlayerIdentity playerIdentity = pair.Value;
                EntityCharacter character = playerIdentity.GetComponent<EntityCharacter>();
                EntitySnapshot snapshot = character.CaptureSnapshot();
                EntitySpawnCommand command = new EntitySpawnCommand
                {
                    entityType = EntityType.Player,
                    position = snapshot.movement.rootPosition,
                    rotation = Quaternion.identity,
                };
                    
                _serverHandler.SendPlayerSpawnNotify(playerId, playerIdentity.EntityId, playerIdentity.PlayerID, command, snapshot);
            }
        }

        #endregion
    }
}