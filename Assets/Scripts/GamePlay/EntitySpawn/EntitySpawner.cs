using System;
using Framework;
using GamePlay.EntityFactory;
using GamePlay.EntitySystem;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.EntitySpawn
{
    /// <summary>
    /// 对局实体生成。主机分配 EntityId 并注册进模拟核；开听后把生成结果发给客户端。客户端只消费生成通知。
    /// </summary>
    public sealed class EntitySpawner : SubSystemBase
    {
        private const float PLAYER_SPACING = 2f;

        private FactoryCommandExecutor _factoryCommandExecutor;
        private EntitySpawnServerHandler _serverHandler;
        private EntitySpawnClientHandler _clientHandler;
        private EntityIdAllocator _allocator;
        
        private Simulator _simulator;
        private RoomManager _room;

        #region 属性
        public override int Priority => 700;
        #endregion

        #region 事件
        public event Action<uint, EntityCharacter> OnSpawnLocalPlayer;
        public event Action<uint, EntityCharacter> OnSpawnRemotePlayer;
        #endregion

        /// <summary>
        /// 对局流程在模拟核启动后绑定本局 Simulator，并给定主机上的实体身份。
        /// </summary>
        public void BindSimulation(Simulator simulator)
        {
            _simulator = simulator;
            _room = Global.Get<RoomManager>();
        }

        /// <summary>
        /// 主机名册已在关卡中，为其中每个玩家生成实体。
        /// </summary>
        public void SpawnRosterPlayers()
        {
            foreach (uint playerId in _room.GetPlayerIds())
            {
                SpawnPlayer(playerId);
            }
        }

        /// <summary>
        /// 主机开听实体消息。此后的生成与销毁才通知其他客户端。
        /// </summary>
        public void StartHost()
        {
            _serverHandler = new EntitySpawnServerHandler(this);
            _serverHandler.Bind();
        }

        /// <summary>
        /// 客户端开始接收生成通知，并向主机请求当前实体。
        /// </summary>
        public void StartClient()
        {
            _clientHandler = new EntitySpawnClientHandler(this);
            _clientHandler.Bind();
            _clientHandler.SendSpawnSyncRequest();
        }

        /// <summary>
        /// 按玩家号把输入来源挂到已生成的玩家实体上。
        /// </summary>
        public void SetPlayerInputSource(uint playerId, IInputStateProvider inputSource)
        {
            uint entityId = _allocator.GetByPlayer(playerId);
            _simulator.SetInputSource(entityId, inputSource);
        }

        /// <summary>
        /// 主机为入座玩家生成实体。已开听时广播给其他客户端。
        /// </summary>
        public void SpawnPlayer(uint playerId)
        {
            uint entityId = _allocator.AllocateByPlayer(playerId, EntityType.Player);
            Vector3 position = new((playerId - 1) * PLAYER_SPACING, 0f, 0f);
            Quaternion rotation = Quaternion.identity;
            DoSpawnPlayer(entityId, EntityType.Player, playerId, position, rotation);

            // 广播实体生成
            _serverHandler?.BroadcastSpawn(entityId, EntityType.Player, playerId, position, rotation);
        }
        
        private void DoSpawnPlayer(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            GameObject instance = _factoryCommandExecutor.Execute(new SpawnEntityCommand(entityTypeId, position, rotation));
            EntityObjectIdentity identity = instance.GetComponent<EntityObjectIdentity>();
            EntityCharacter character = instance.GetComponent<EntityCharacter>();
            identity.Init(entityId, playerId, ResolveRole(playerId));
            character.Init();
            
            _simulator.Register(identity, character);
            
            if (playerId == _room.LocalPlayerId)
            {
                OnSpawnLocalPlayer?.Invoke(playerId, character);
            }
            else if (_room.SessionRole == SessionRole.Host)
            {
                OnSpawnRemotePlayer?.Invoke(playerId, character);
            }
        }

        /// <summary>
        /// 主机移除玩家实体。已开听时通知其他客户端。
        /// </summary>
        public void DespawnPlayer(uint playerId)
        {
            uint entityId = _allocator.GetByPlayer(playerId);
            Despawn(entityId);
            
            _serverHandler?.BroadcastDestroy(entityId);  
        }
        
        public void Despawn(uint entityId)
        {
            _simulator.TryGet(entityId, out _, out EntityCharacter character);
            _simulator.Unregister(entityId);
            _allocator.Remove(entityId);
            _factoryCommandExecutor.Execute(new DestroyEntityCommand(character.gameObject));
        }
        
        /// <summary>
        /// 清掉本局已生成的实体。须在模拟核销毁之前调用。
        /// </summary>
        public void DespawnAll()
        {
            foreach (var record in _allocator.GetLiveEntityIds())
            {
                Despawn(record.EntityId);
            }
        }
        
        private EntityObjectRole ResolveRole(uint playerId)
        {
            if (_room.SessionRole == SessionRole.Client)
            {
                return playerId == _room.LocalPlayerId ? EntityObjectRole.Predict : EntityObjectRole.Replica;
            }

            return _room.AcceptsRemoteJoin ? EntityObjectRole.Authority : EntityObjectRole.LocalPlay;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _allocator = new EntityIdAllocator();
            _factoryCommandExecutor = new FactoryCommandExecutor();
        }

        public override void Destroy()
        {
            if (_serverHandler != null)
            {
                _serverHandler.Unbind();
                _serverHandler = null;
            }
            
            if (_clientHandler != null)
            {
                _clientHandler.Unbind();
                _clientHandler = null;
            }
            
            DespawnAll();
            _factoryCommandExecutor = null;
            _allocator = null;
            _serverHandler = null;
            _clientHandler = null;
            _simulator = null;
            _room = null;
        }

        #endregion

        #region 消息发送

        private void SendSpawnSnapshot(uint connectionId)
        {
            foreach (var record in _allocator.GetLiveEntityIds())
            {
                _simulator.TryGet(record.EntityId, out _, out EntityCharacter character);
                uint playerId = _allocator.GetPlayerId(record.EntityId);
                _serverHandler.SendSpawn(connectionId, record.EntityId, record.EntityTypeId, playerId, 
                    character.transform.position, character.transform.rotation);
            }
        }

        #endregion

        #region 网络消息处理

        public void HandleSpawnNotify(uint entityId, EntityType entityTypeId, uint playerId, Vector3 position, Quaternion rotation)
        {
            if (_simulator.TryGet(entityId, out _, out _)) return;

            _allocator.AdoptByPlayer(playerId, entityId, entityTypeId);
            DoSpawnPlayer(entityId, entityTypeId, playerId, position, rotation);
        }

        public void HandleDestroyNotify(uint entityId)
        {
            Despawn(entityId);
        }

        public void HandleSpawnSyncRequest(uint connectionId)
        {
            SendSpawnSnapshot(connectionId);
        }

        #endregion
    }
}
