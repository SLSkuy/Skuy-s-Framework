using System;
using Framework;
using GamePlay.DataProxy;
using GamePlay.Room;
using GamePlay.Simulation;
using NetSync;

namespace GamePlay.BattleSystem
{
    public class BattleManager : SubSystemBase
    {
        private BattleClientHandler _clientHandler;
        private BattleServerHandler _serverHandler;
        
        private BattleEntityManager _entityManager;
        private ISimulationKernel _simulator;
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
            _entityManager = new BattleEntityManager(_simulator);
            if (!_room.IsHost)
            {
                // 非主机客户端向服务端请求玩家实体，本地不做生成
                _clientHandler.SendSpawnSyncRequest();
                return;
            }
            
            // 主机端本地玩家加入房间
            _entityManager.OnPlayerJoin(Global.GetDataProxy<PlayerInfoDataProxy>().UUID);

            _room.PlayerJoined += _entityManager.OnPlayerJoin;
            _room.PlayerLeft += _entityManager.OnPlayerLeave;
        }
        
        private void DestroyEntitySystem()
        {
            if(_room.IsHost) _entityManager.RestoreAllEntity();
            _entityManager.Dispose();

            _room.PlayerJoined -= _entityManager.OnPlayerJoin;
            _room.PlayerLeft -= _entityManager.OnPlayerLeave;
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            _room = Global.Get<RoomManager>();
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
            ((ClientSimulationKernel)_simulator).HandleWorldSnapshot(snapshot);
        }

        #endregion

        #region 服务端消息处理

        public void HandlePlayerInput(Guid playerId, Player_Inputs input)
        {
            ((HostSimulationKernel)_simulator).HandlePlayerInput(playerId, input);
        }
        
        #endregion
    }
}
