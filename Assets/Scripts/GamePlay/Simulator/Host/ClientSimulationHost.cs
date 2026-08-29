using System.Collections.Generic;
using Core;
using Framework;
using GamePlay.EntitySystem;
using UnityEngine;
using Utils;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 客户端会话：持核、采样本机输入发送、快照生成 Replica 并 Restore
    /// </summary>
    public sealed class ClientSimulationHost : SubSystemBase
    {
        private readonly Dictionary<uint, GameObject> _pawns = new();
        private readonly ClientHandler _handler = new();
        private Simulator _simulator;
        private GameObject _playerPrefab;
        private uint _inputTick;
        private uint _lastAppliedSnapshotTick;
        private uint _ownedEntityId;
        private bool _joinRequested;
        private bool _joinAccepted;
        private bool _cameraBound;

        #region 属性
        public override int Priority => 500;
        public Simulator Simulator => _simulator;
        public bool IsSessionRunning { get; private set; }
        public uint ClientId => _handler.ClientId;
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;

            _playerPrefab = PlayerSpawner.LoadPrefab();
            _handler.OnJoinResponse += HandleJoinResponse;
            _handler.OnWorldSnapshot += HandleWorldSnapshot;
            if (!_handler.BindMessage())
            {
                _handler.OnJoinResponse -= HandleJoinResponse;
                _handler.OnWorldSnapshot -= HandleWorldSnapshot;
                return false;
            }
            _simulator.Tick += HandleSimulatorTick;
            _simulator.StartClock();
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.Tick -= HandleSimulatorTick;
            _handler.UnbindMessage();
            _handler.OnJoinResponse -= HandleJoinResponse;
            _handler.OnWorldSnapshot -= HandleWorldSnapshot;
            ClearReplicas();
            _simulator.StopClock();
            UnlockCursor();
            _inputTick = 0;
            _lastAppliedSnapshotTick = 0;
            _ownedEntityId = 0;
            _joinRequested = false;
            _joinAccepted = false;
            _cameraBound = false;
            IsSessionRunning = false;
        }

        private void TrySendJoin()
        {
            if (!IsSessionRunning || _joinRequested || ClientId == 0) return;
            _joinRequested = true;
            _handler.SendJoinRequest();
        }

        private void HandleJoinResponse(global::NetSync.Game_Join_Response response)
        {
            if (response == null || !response.Accepted)
            {
                _joinRequested = false;
                _joinAccepted = false;
                return;
            }

            _joinAccepted = true;
            _ownedEntityId = ClientId;
        }

        private void HandleSimulatorTick(uint tick, float deltaTime)
        {
            if (!_joinAccepted || _ownedEntityId == 0 || _lastAppliedSnapshotTick == 0) return;
            if (GameCore.Instance == null) return;

            _inputTick++;
            _handler.SendPlayerInput(_inputTick, GameCore.Instance.LocalInput.GetInputState());
        }

        private void HandleWorldSnapshot(global::NetSync.World_Snapshot world)
        {
            if (world == null || world.SnapshotTick <= _lastAppliedSnapshotTick) return;
            _lastAppliedSnapshotTick = world.SnapshotTick;

            foreach (global::NetSync.Character_Snapshot snapshot in world.CharacterSnapshots)
            {
                ApplyCharacterSnapshot(snapshot);
            }
        }

        private void ApplyCharacterSnapshot(global::NetSync.Character_Snapshot snapshot)
        {
            if (snapshot == null || snapshot.EntityId == 0) return;

            if (!_simulator.TryGet(snapshot.EntityId, out _, out _))
            {
                SpawnReplica(snapshot.EntityId, snapshot.OwnerClientId);
            }

            EntityRollbackState state = ProtoUtils.ToRollbackState(snapshot);
            _simulator.TryRestore(snapshot.EntityId, state);
            TryBindOwnedCamera(snapshot.EntityId, snapshot.OwnerClientId);
        }

        private void SpawnReplica(uint entityId, uint ownerClientId)
        {
            if (_pawns.ContainsKey(entityId)) return;

            bool isOwned = ownerClientId != 0 && ownerClientId == ClientId;
            string objectName = isOwned ? $"LocalPlayer_{entityId}" : $"RemotePlayer_{entityId}";
            EntityObjectIdentity identity = PlayerSpawner.Spawn(
                _playerPrefab, Vector3.up, objectName, entityId, EntityObjectRole.Replica, ownerClientId);
            _simulator.Register(identity, identity.GetComponent<EntityCharacter>());
            _pawns[entityId] = identity.gameObject;
        }

        private void TryBindOwnedCamera(uint entityId, uint ownerClientId)
        {
            if (_cameraBound) return;
            if (ownerClientId == 0 || ownerClientId != ClientId) return;
            if (!_pawns.TryGetValue(entityId, out GameObject pawn) || pawn == null) return;

            GameCore.Instance.CameraMgr.SetTarget(pawn.transform.Find("orientation"));
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cameraBound = true;
        }

        private void ClearReplicas()
        {
            foreach (uint entityId in _pawns.Keys)
            {
                _simulator.Unregister(entityId);
            }

            foreach (GameObject pawn in _pawns.Values)
            {
                if (pawn != null) Object.Destroy(pawn);
            }

            _pawns.Clear();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
            TrySendJoin();
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
