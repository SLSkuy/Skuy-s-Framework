using Framework;
using GamePlay.EntitySpawn;
using GamePlay.EntitySystem;
using GamePlay.LevelControl;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.Procedure
{
    /// <summary>
    /// 对局流程状态，粘合其他的所有游戏功能模块
    /// </summary>
    public sealed class ProcedureMatchState : EnumStateBase<ProcedureState>
    {
        private readonly ProcedureCore _procedure;
        private ISimulationKernel _simulation;
        
        #region 游戏业务逻辑
        private EntitySpawner _entitySpawner;
        private LevelManager _levelMgr;
        private RoomManager _room;
        #endregion

        public ProcedureMatchState(EnumStateMachine<ProcedureState> stateMachine, ProcedureCore procedure) : base(stateMachine)
        {
            _procedure = procedure;
        }

        public override int StateKey => (int)ProcedureState.Match;
        
        private bool StartGameplay()
        {
            // 注册房间管理器
            _room = Global.Get<RoomManager>();
            
            // 注册模拟核，确定如何进行游戏逻辑Tick
            SessionRole sessionRole = _room.SessionRole;
            SystemManager systems = Global.Get<SystemManager>();
            ISimulationKernel kernel = sessionRole == SessionRole.Client ? new ClientSimulationKernel() : new HostSimulationKernel();
            systems.RegisterSystem(kernel);
            if (!kernel.StartSession())
            {
                systems.UnregisterSystem(kernel);
                return false;
            }
            _simulation = kernel;
            
            // 注册实体生成管理器
            _entitySpawner = Global.Register<EntitySpawner>();
            _entitySpawner.BindSimulation(_simulation.SimulationKernal);
            _entitySpawner.OnSpawnLocalPlayer += OnLocalPlayerSpawned;
            if (sessionRole == SessionRole.Host)
            {
                _entitySpawner.OnSpawnRemotePlayer += OnRemotePlayerSpawned;
            }
            
            // 注册关卡管理器
            _levelMgr = Global.Register<LevelManager>();
            _levelMgr.Completed += OnLevelLoadCompleted;
            _levelMgr.Failed += OnLevelLoadFailed;
            _levelMgr.LoadMatchLevel();
            
            return true;
        }

        private void StopGameplay()
        {
            if (_room != null)
            {
                _room.PlayerJoined -= OnPlayerJoined;
                _room.PlayerRemoved -= OnPlayerRemoved;
                _room = null;
            }
            
            if (_entitySpawner != null)
            {
                _entitySpawner.OnSpawnLocalPlayer -= OnLocalPlayerSpawned;
                _entitySpawner.OnSpawnRemotePlayer -= OnRemotePlayerSpawned;
                Global.Unregister<EntitySpawner>();
                _entitySpawner = null;
            }

            Global.Get<CameraManager>().SetTarget(null);
            
            if (_simulation != null)
            {
                _simulation.StopSession();
                Global.Get<SystemManager>().UnregisterSystem(_simulation);
                _simulation = null;
            }
            
            if (_levelMgr != null)
            {
                _levelMgr.Completed -= OnLevelLoadCompleted;
                _levelMgr.Failed -= OnLevelLoadFailed;
                Global.Unregister<LevelManager>();
                _levelMgr = null;
            }
        }

        #region 状态周期

        public override void Enter()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            if (!StartGameplay())
            {
                _stateMachine.ChangeState(ProcedureState.Menu);
            }
        }

        public override void Exit()
        {
            StopGameplay();
        }

        #endregion

        #region 事件回调
        
        /// <summary>
        /// 生成本机玩家，绑定输入来源，设置摄像机跟随目标
        /// </summary>
        private void OnLocalPlayerSpawned(uint playerId, EntityCharacter character)
        {
            if (_room.SessionRole == SessionRole.Host)
            {
                // 客户端现阶段不做预测输入
                _entitySpawner.SetPlayerInputSource(playerId, Global.Get<LocalInputManager>().Provider);    
            }
            
            Global.Get<CameraManager>().SetTarget(character.Context.View.Orientation);
        }

        /// <summary>
        /// 主机上的远端玩家生成后，绑定网络输入来源。
        /// </summary>
        private void OnRemotePlayerSpawned(uint playerId, EntityCharacter _)
        {
            Simulator simulator = _simulation.SimulationKernal;
            AuthorityInputProvider provider = new(simulator.MaxBufferedInputs, simulator.MaxFutureInputTicks);
            _entitySpawner.SetPlayerInputSource(playerId, provider);
            
            ((HostSimulationKernel)_simulation).RegisterRemoteInput(playerId, provider);
        }

        private void OnLevelLoadCompleted(string sceneName)
        {
            if (_room.SessionRole == SessionRole.Client)
            {
                _entitySpawner.StartClient();
                return;
            }

            _entitySpawner.SpawnRosterPlayers();
            if (_room.AcceptsRemoteJoin)
            {
                _entitySpawner.StartHost();
            }

            _room.PlayerJoined += OnPlayerJoined;
            _room.PlayerRemoved += OnPlayerRemoved;
        }

        private void OnLevelLoadFailed(string sceneName, string errorMessage)
        {
            _stateMachine.ChangeState(ProcedureState.Menu);
        }

        private void OnPlayerJoined(uint playerId)
        {
            _entitySpawner.SpawnPlayer(playerId);
        }

        private void OnPlayerRemoved(uint playerId)
        {
            ((HostSimulationKernel)_simulation).UnregisterRemoteInput(playerId);
            
            _entitySpawner.DespawnPlayer(playerId);
        }

        #endregion
    }
}
