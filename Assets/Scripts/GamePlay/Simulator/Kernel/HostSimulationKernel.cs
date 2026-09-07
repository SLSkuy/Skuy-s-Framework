using Framework;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 主机权威模拟核：持有 Simulator，开战时生成并附身本地角色。
    /// </summary>
    public sealed class HostSimulationKernel : SubSystemBase, ISimulationKernel
    {
        private readonly uint _hostPlayerId;
        private readonly EntityObjectRole _pawnRole;
        private Simulator _simulator;
        private GameObject _pawn;

        #region 属性
        public override int Priority => 500;
        public Simulator Simulator => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public HostSimulationKernel(uint hostPlayerId, EntityObjectRole pawnRole)
        {
            _hostPlayerId = hostPlayerId;
            _pawnRole = pawnRole;
        }

        public bool StartSession()
        {
            if (IsSessionRunning) return true;

            GameObject prefab = PlayerSpawner.LoadPrefab();
            if (!prefab) return false;

            EntityObjectIdentity identity = PlayerSpawner.Spawn(prefab, Vector3.up,
                "LocalPlayer", _hostPlayerId, _pawnRole, 0);
            _pawn = identity.gameObject;

            _simulator.Register(identity, identity.GetComponent<EntityCharacter>());
            _simulator.SetInputSource(_hostPlayerId, Global.Get<LocalInputManager>().Provider);
            _simulator.StartClock();

            Global.Get<CameraManager>().SetTarget(_pawn.transform.Find("orientation"));
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.SetInputSource(_hostPlayerId, null);
            _simulator.Unregister(_hostPlayerId);
            _simulator.StopClock();
            Object.Destroy(_pawn);
            _pawn = null;

            IsSessionRunning = false;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _simulator = new Simulator();
            _simulator.Init();
        }

        public override void Update(float deltaTime)
        {
            _simulator.Update(deltaTime);
        }

        public override void Destroy()
        {
            StopSession();
            _simulator.Destroy();
            _simulator = null;
        }

        #endregion

        #region 网络消息处理

        public void HandlePlayerInput()
        {
            
        }

        #endregion
    }
}
