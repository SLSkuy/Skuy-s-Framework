using Core;
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
        private const uint LOCAL_PLAYER_ID = 1;

        private Simulator _simulator;
        private GameObject _pawn;

        #region 属性
        public override int Priority => 500;
        public Simulator Simulator => _simulator;
        public bool IsSessionRunning { get; private set; }
        public uint LocalEntityId => IsSessionRunning ? LOCAL_PLAYER_ID : 0;
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            if (!GameCore.Instance) return false;

            GameObject prefab = PlayerSpawner.LoadPrefab();
            if (!prefab) return false;

            EntityObjectIdentity identity = PlayerSpawner.Spawn(prefab, Vector3.up,
                "LocalPlayer", LOCAL_PLAYER_ID, EntityObjectRole.LocalPlay, 0);
            _pawn = identity.gameObject;
            
            _simulator.Register(identity, identity.GetComponent<EntityCharacter>());
            _simulator.SetInputSource(LOCAL_PLAYER_ID, GameCore.Instance.LocalInput);
            _simulator.StartClock();

            GameCore.Instance.CameraMgr.SetTarget(_pawn.transform.Find("orientation"));
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.SetInputSource(LOCAL_PLAYER_ID, null);
            _simulator.Unregister(LOCAL_PLAYER_ID);
            _simulator.StopClock();
            Object.Destroy(_pawn);
            _pawn = null;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            IsSessionRunning = false;
        }

        #region 生命周期

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
    }
}
