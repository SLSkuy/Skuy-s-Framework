using Framework;
using GamePlay.EntitySystem;
using GamePlay.Room;
using GamePlay.Simulation;
using UnityEngine;
using YooAsset;

namespace GamePlay.GameCore
{
    /// <summary>
    /// 对局期本机 pawn 子模块：关卡完成后由对局流程态转发，不订阅场景加载器。
    /// </summary>
    public sealed class LocalPawnModule : SubSystemBase
    {
        private Simulator _simulator;
        private GameObject _instance;
        private SessionRole _sessionRole;
        private uint _entityId;

        #region 属性
        public override int Priority => 210;
        #endregion
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 生成玩家实体
        /// </summary>
        private void SpawnPlayerEntity()
        {
            if (_sessionRole == SessionRole.Client) return;
            if (_instance) return;
            
            uint hostPlayerId = Global.Get<RoomManager>().HostPlayerId;
            GameObject instance = Global.Instantiate("Entity_NetPlayer", 
                new InstantiateOptions(true, Vector3.zero, Quaternion.identity));
            
            EntityObjectIdentity identity = instance.GetComponent<EntityObjectIdentity>();
            EntityCharacter character = instance.GetComponent<EntityCharacter>();
            identity.Init(hostPlayerId, 0, EntityObjectRole.Authority);
            character.Init();
            
            _simulator.Register(identity, character);
            _simulator.SetInputSource(hostPlayerId, Global.Get<LocalInputManager>().Provider);

            // 绑定摄像机
            Global.Get<CameraManager>().SetTarget(instance.transform.Find("orientation"));
            
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _instance = instance;
            _entityId = hostPlayerId;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _sessionRole = Global.Get<RoomManager>().SessionRole;
            if (_sessionRole == SessionRole.Client)
            {
                return;
            }

            _simulator = Global.Get<HostSimulationKernel>().SimulationKernal;
        }

        public override void Destroy()
        {
            if (_instance == null)
            {
                return;
            }
            
            _simulator.SetInputSource(_entityId, null);
            _simulator.Unregister(_entityId);
            
            Global.Release(_instance);
            _instance = null;
            _entityId = 0;
        }

        #endregion

        #region 事件回调

        public void HandleMatchLevelCompleted()
        {
            SpawnPlayerEntity();
        }

        #endregion
    }
}
