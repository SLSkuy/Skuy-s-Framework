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
            SessionRole role = _room.Role;
            if (role == SessionRole.Host)
            {
                _serverHandler = new BattleServerHandler(this);
                _simulator = new HostSimulationKernel(_serverHandler);
                _serverHandler.Bind();
            }else if (role == SessionRole.Client)
            {
                _clientHandler = new BattleClientHandler(this);
                _simulator = new ClientSimulationKernel(_clientHandler);
                _serverHandler.Bind();
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
            // 生成本地主控玩家
            Guid localPlayerUUID = _playerData.UUID;
            var entityIdentity = _entityFactory.SpawnNetEntity(EntityType.Player);
            var entityCharacter = entityIdentity.GetComponent<EntityCharacter>();
            var playerSnapshot = _saveData.GetPlayerSnapshot(localPlayerUUID);
            
            entityCharacter.Init();
            entityCharacter.RestoreSnapshot(playerSnapshot);

            if (entityIdentity is PlayerIdentity playerIdentity)
            {
                playerIdentity.BindPlayerID(_playerData.UUID);
                Global.Get<CameraManager>().SetTarget(entityCharacter.Context.View.Orientation);
                _simulator.Kernel.Register(playerIdentity, entityCharacter);
                _simulator.Kernel.SetInputSource(playerIdentity.EntityId, Global.Get<LocalInputManager>().Provider);
            }
            // 生成本地主控玩家
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

        public void HandleSpawnNotify(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            // if (_simulator.TryGet(entityId, out _, out _)) return;
            //
            // _allocator.AdoptByPlayer(playerId, entityId, entityTypeId);
            // DoSpawnPlayer(entityId, entityTypeId, playerId, position, rotation);
        }

        public void HandleDestroyNotify(uint entityId)
        {
            _entityFactory.DestroyEntity(entityId);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(Guid playerId, Player_Input input)
        {
            if (_simulator is HostSimulationKernel kernel)
            {
                kernel.HandlePlayerInput(playerId, input);
            }
        }

        public void HandleSpawnSyncRequest(Guid playerId)
        {
            foreach (var record in _entityFactory.GetActiveEntities())
            {
                // _simulator.TryGet(record.EntityId, out EntityIdentity identity, out EntityCharacter character);
                // _serverHandler.SendSpawnNotify(playerId, record.EntityId, record.EntityTypeId, identity.PlayerId,
                // character.transform.position, character.transform.rotation);
            }
        }

        #endregion
    }
}