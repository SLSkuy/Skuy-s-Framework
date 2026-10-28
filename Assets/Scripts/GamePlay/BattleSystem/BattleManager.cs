using System;
using System.Collections.Generic;
using Framework;
using GamePlay.DataProxy;
using GamePlay.EntitySpawn;
using GamePlay.EntitySystem;
using GamePlay.Room;
using GamePlay.Simulation;
using NetSync;
using UnityEngine;

namespace GamePlay.BattleSystem
{
    public class BattleManager : SubSystemBase
    {
        private readonly PlayerInfoDataProxy _playerData = Global.GetDataProxy<PlayerInfoDataProxy>();
        private readonly GameSaveDataProxy _saveData = Global.GetDataProxy<GameSaveDataProxy>();
        private readonly Dictionary<Guid, PlayerIdentity> _spawnedPlayers = new();
        
        private BattleClientHandler _clientHandler;
        private BattleServerHandler _serverHandler;
        
        private ISimulationKernel _simulator;
        private HostSimulationKernel _hostKernel;
        private EntityFactory _entityFactory;
        private RoomManager _room;
        
        #region 属性
        public override int Priority => (int)GamePlaySystemPriority.BattleManager;
        public bool IsRunning => _simulator?.IsSessionRunning ?? false;
        #endregion

        public void InitBattle()
        {
            InitSimulationKernel();
            InitWorldMap();
            InitEntitySystem();
        }

        public void StopBattle()
        {
            if (!IsRunning) return;
            
            DestroySimulationKernel();
            DestroyEntitySystem();
        }

        #region 模拟核管理

        /// <summary>
        /// 初始化网络同步模拟核
        /// </summary>
        private void InitSimulationKernel()
        {
            SessionRole role = _room.Role;
            if (role == SessionRole.Host)
            {
                _serverHandler = new BattleServerHandler(this);
                _hostKernel = new HostSimulationKernel(_serverHandler);
                _simulator = _hostKernel;
                _serverHandler.Bind();
            }
            else if (role == SessionRole.Client)
            {
                _clientHandler = new BattleClientHandler(this);
                _simulator = new ClientSimulationKernel(_clientHandler);
                _clientHandler.Bind();
            }
            
            _simulator.StartSession();
        }
        
        private void DestroySimulationKernel()
        {
            _simulator?.Dispose();
            _simulator = null;
            _hostKernel = null;

            if (_clientHandler != null)
            {
                _clientHandler.Unbind();
                _clientHandler = null;
            }

            if (_serverHandler != null)
            {
                _serverHandler.Unbind();
                _serverHandler = null;
            }
        }

        #endregion

        #region 地图管理

        private void InitWorldMap()
        {
            // TODO: 加载生成地图
        }

        #endregion

        #region 实体管理

        /// <summary>
        /// 初始化实体系统，生成本机玩家，监听远端实体生成命令
        /// </summary>
        private void InitEntitySystem()
        {
            if (!_room.IsHost)
            {
                // 非主机客户端向服务端请求玩家实体，本地不做生成
                _clientHandler.SendSpawnSyncRequest();
                return;
            }
            
            Guid localPlayerId = _playerData.UUID;
            EntitySnapshot playerSnapshot = _saveData.GetPlayerSnapshot(localPlayerId);
            SpawnHostPlayer(localPlayerId, playerSnapshot, useLocalInput: true);

            _room.PlayerJoined += OnPlayerJoin;
            _room.PlayerLeft += OnPlayerLeave;
        }

        private void OnPlayerJoin(Guid playerId)
        {
            EntitySnapshot playerSnapshot = _saveData.GetPlayerSnapshot(playerId);
            EntityIdentity identity = SpawnHostPlayer(playerId, playerSnapshot, useLocalInput: false);
            _hostKernel.RegisterRemoteInput(playerId, identity.EntityId);
            _serverHandler.BroadcastSpawnNotify(identity.EntityId, playerId, CreatePlayerSpawnCommand(playerSnapshot), playerSnapshot);
        }

        private void OnPlayerLeave(Guid playerId)
        {
            PlayerIdentity playerIdentity = _spawnedPlayers[playerId];
            EntityCharacter character = playerIdentity.GetComponent<EntityCharacter>();
            _saveData.RestorePlayerSnapshot(playerId, character.CaptureSnapshot());

            _simulator.Kernel.Unregister(playerIdentity.EntityId);
            _hostKernel.UnregisterRemoteInput(playerId);
            _spawnedPlayers.Remove(playerId);

            EntityDestroyCommand command = new EntityDestroyCommand
            {
                entityId = playerIdentity.EntityId,
            };
            _entityFactory.DestroyEntity(playerIdentity.EntityId);
            _serverHandler.BroadcastDestroyNotify(command);
        }

        private EntityIdentity SpawnHostPlayer(Guid playerId, in EntitySnapshot snapshot, bool useLocalInput)
        {
            EntityIdentity identity = _entityFactory.SpawnNetEntity(CreatePlayerSpawnCommand(snapshot));
            AssemblePlayer(identity, playerId, snapshot, EntityRole.Authority, useLocalInput);
            return identity;
        }

        private void AssemblePlayer(EntityIdentity identity, Guid playerId, in EntitySnapshot snapshot,
            EntityRole role, bool useLocalInput)
        {
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            character.Init();
            character.RestoreSnapshot(snapshot);

            PlayerIdentity playerIdentity = (PlayerIdentity)identity;
            playerIdentity.BindPlayerID(playerId);
            playerIdentity.BindEntityRole(role);
            _spawnedPlayers.Add(playerId, playerIdentity);
            _simulator.Kernel.Register(playerIdentity, character);

            if (!useLocalInput) return;

            Global.Get<CameraManager>().SetTarget(character.Context.View.Orientation);
            _simulator.Kernel.SetInputSource(playerIdentity.EntityId, Global.Get<LocalInputManager>().Provider);
        }

        private static EntitySpawnCommand CreatePlayerSpawnCommand(in EntitySnapshot snapshot)
        {
            return new EntitySpawnCommand
            {
                entityTypeId = EntityType.Player,
                position = snapshot.movement.rootPosition,
                rotation = Quaternion.identity,
            };
        }

        private void RemoveSpawnedPlayer(uint entityId)
        {
            Guid playerId = default;
            bool found = false;
            foreach (var pair in _spawnedPlayers)
            {
                if (pair.Value.EntityId != entityId) continue;
                playerId = pair.Key;
                found = true;
                break;
            }

            if (found)
            {
                _spawnedPlayers.Remove(playerId);
            }
        }

        private void DestroyEntitySystem()
        {
            foreach (var pair in _spawnedPlayers)
            {
                EntityCharacter character = pair.Value.GetComponent<EntityCharacter>();
                _saveData.RestorePlayerSnapshot(pair.Key, character.CaptureSnapshot());
            }

            _spawnedPlayers.Clear();
            _entityFactory.DestroyAll();
            _entityFactory.Dispose();
            _entityFactory = null;
            
            _room.PlayerJoined -= OnPlayerJoin;
            _room.PlayerLeft -= OnPlayerLeave;
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            _room = Global.Get<RoomManager>();
            _entityFactory = new EntityFactory();
        }

        public override void Update(float deltaTime)
        {
            _simulator?.Update(deltaTime);
        }

        public override void Destroy()
        {
            StopBattle();
        }

        #endregion
        
        #region 客户端消息处理

        public void HandleWorldSnapshot(World_Snapshot snapshot)
        {
            if (_simulator is ClientSimulationKernel kernel)
            {
                kernel.HandleWorldSnapshot(snapshot);
            }
        }

        public void HandleSpawnNotify(uint entityId, Guid playerId, in EntitySpawnCommand command,
            in EntitySnapshot snapshot)
        {
            if (_entityFactory.TryGetEntity(entityId, out EntityIdentity existing))
            {
                existing.GetComponent<EntityCharacter>().RestoreSnapshot(snapshot);
                return;
            }

            EntityIdentity identity = _entityFactory.HandleSpawnEntityCommand(entityId, command);
            bool isLocal = playerId == _playerData.UUID;
            EntityRole role = isLocal ? EntityRole.Predict : EntityRole.Replica;
            AssemblePlayer(identity, playerId, snapshot, role, isLocal);
        }

        public void HandleDestroyNotify(in EntityDestroyCommand command)
        {
            _simulator.Kernel.Unregister(command.entityId);
            RemoveSpawnedPlayer(command.entityId);
            _entityFactory.DestroyEntity(command.entityId);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(Guid playerId, Player_Inputs input)
        {
            _hostKernel.HandlePlayerInput(playerId, input);
        }

        public void HandleSpawnSyncRequest(Guid playerId)
        {
            foreach (var pair in _spawnedPlayers)
            {
                PlayerIdentity playerIdentity = pair.Value;
                EntityCharacter character = playerIdentity.GetComponent<EntityCharacter>();
                EntitySnapshot snapshot = character.CaptureSnapshot();
                EntitySpawnCommand command = CreatePlayerSpawnCommand(snapshot);
                _serverHandler.SendSpawnNotify(playerId, playerIdentity.EntityId, playerIdentity.PlayerID, command, snapshot);
            }
        }

        #endregion
    }
}
