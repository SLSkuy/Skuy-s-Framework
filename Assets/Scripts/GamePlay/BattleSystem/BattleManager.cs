using System;
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
        
        private BattleClientHandler _clientHandler;
        private BattleServerHandler _serverHandler;
        
        private ISimulationKernel _simulator;
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
            if (_room.IsHost)
            {
                _serverHandler = new BattleServerHandler(this);
                _simulator = new HostSimulationKernel(_serverHandler);
                _serverHandler.Bind();
            }else if (_room.IsClient)
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
            
            // BattleManager负责绑定玩家ID和实体角色，实体工厂绑定实体ID和实体类型
            // 生成本地主控玩家
            Guid localPlayerUUID = _playerData.UUID;
            EntityIdentity entityIdentity = _entityFactory.SpawnNetEntity(EntityType.Player);
            EntityCharacter entityCharacter = entityIdentity.GetComponent<EntityCharacter>();
            EntitySnapshot playerSnapshot = _saveData.GetPlayerSnapshot(localPlayerUUID);
            
            entityCharacter.Init();
            entityCharacter.RestoreSnapshot(playerSnapshot);

            if (entityIdentity is PlayerIdentity playerIdentity)
            {
                playerIdentity.BindPlayerID(_playerData.UUID);
                playerIdentity.BindEntityRole(EntityRole.Authority);
                Global.Get<CameraManager>().SetTarget(entityCharacter.Context.View.Orientation);
                _simulator.Kernel.Register(playerIdentity, entityCharacter);
                _simulator.Kernel.SetInputSource(playerIdentity.EntityId, Global.Get<LocalInputManager>().Provider);
            }
            // 生成本地主控玩家

            _room.PlayerJoined += OnPlayerJoin;
            _room.PlayerLeft += OnPlayerLeave;
        }

        private void OnPlayerJoin(Guid uuid)
        {
            // 生成远端加入玩家实体
            EntitySnapshot playerSnapshot = _saveData.GetPlayerSnapshot(uuid);
            EntitySpawnCommand command = new EntitySpawnCommand
            {
                entityTypeId = EntityType.Player,
                position = playerSnapshot.movement.rootPosition,
                rotation = Quaternion.identity, // 通过快照设置模型旋转，根节点不做旋转
            };
            
            EntityIdentity entityIdentity = _entityFactory.SpawnNetEntity(command);
            EntityCharacter character = entityIdentity.GetComponent<EntityCharacter>();
            
            character.Init();
            character.RestoreSnapshot(playerSnapshot);
            if (entityIdentity is PlayerIdentity playerIdentity)
            {
                playerIdentity.BindPlayerID(uuid);
                playerIdentity.BindEntityRole(EntityRole.Authority);
                _simulator.Kernel.Register(playerIdentity, character);
                ((HostSimulationKernel)_simulator).RegisterRemoteInput(uuid, playerIdentity.EntityId);
            }
            
            _serverHandler.BroadcastSpawnNotify(entityIdentity.EntityId, uuid, command);
        }

        private void OnPlayerLeave(Guid uuid)
        {
            // TODO: 改成字典查询，而不是循环查找
            // TODO: 销毁玩家实体
            foreach (var identity in _entityFactory.GetActiveEntities())
            {
                if (identity is not PlayerIdentity playerIdentity) continue;
                if (playerIdentity.PlayerID == uuid)
                {
                    _simulator.Kernel.Unregister(playerIdentity.EntityId);
                    ((HostSimulationKernel)_simulator).UnregisterRemoteInput(uuid);
                    EntityDestroyCommand command = new EntityDestroyCommand
                    {
                        entityId = playerIdentity.EntityId,
                    };
                    _entityFactory.RequireDestroyEntity(command);
                    _serverHandler.BroadcastDestroyNotify(command);
                    
                    EntityCharacter entityCharacter = playerIdentity.GetComponent<EntityCharacter>();
                    _saveData.RestorePlayerSnapshot(uuid, entityCharacter.CaptureSnapshot());
                    break;
                }
            }
        }

        private void DestroyEntitySystem()
        {
            // 存储所有实体信息
            foreach (var identity in _entityFactory.GetActiveEntities())
            {
                if (identity is PlayerIdentity playerIdentity)
                {
                    EntityCharacter entityCharacter = playerIdentity.GetComponent<EntityCharacter>();
                    EntitySnapshot snapshot = entityCharacter.CaptureSnapshot();
                    _saveData.RestorePlayerSnapshot(playerIdentity.PlayerID, snapshot);
                }
            }
            
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
            _entityFactory?.Update();
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

        public void HandleSpawnNotify(uint entityId, Guid playerId, in EntitySpawnCommand command)
        {
            EntityIdentity identity = _entityFactory.HandleSpawnEntityCommand(entityId, command);
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            character.Init();
            
            if (identity is PlayerIdentity playerIdentity)
            {
                playerIdentity.BindPlayerID(playerId);  // 绑定玩家身份ID
                _simulator.Kernel.Register(playerIdentity, character);
                
                if (_playerData.UUID == playerId)
                {
                    // 客户端主控玩家，设置玩家控制器
                    playerIdentity.BindEntityRole(EntityRole.Predict);
                    Global.Get<CameraManager>().SetTarget(character.Context.View.Orientation);
                    _simulator.Kernel.SetInputSource(entityId, Global.Get<LocalInputManager>().Provider);
                }
                else
                {
                    playerIdentity.BindEntityRole(EntityRole.Replica);
                }
            }
        }

        public void HandleDestroyNotify(in EntityDestroyCommand command)
        {
            // 注销实体模拟
            _simulator.Kernel.Unregister(command.entityId);
            _entityFactory.HandleDestroyEntityCommand(command);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(Guid playerId, Player_Input input)
        {
            ((HostSimulationKernel)_simulator).HandlePlayerInput(playerId, input);
        }

        public void HandleSpawnSyncRequest(Guid playerId)
        {
            foreach (var identity in _entityFactory.GetActiveEntities())
            {
                _simulator.Kernel.TryGet(identity.EntityId, out _, out var character);
                if (identity is PlayerIdentity playerIdentity)
                {
                    EntitySpawnCommand command = new EntitySpawnCommand
                    {
                        entityTypeId = playerIdentity.EntityType,
                        position =  character.transform.position,
                        rotation = character.transform.rotation,
                    };
                    _serverHandler.SendSpawnNotify(playerId, playerIdentity.EntityId, playerIdentity.PlayerID, command);
                }
            }
        }

        #endregion
    }
}