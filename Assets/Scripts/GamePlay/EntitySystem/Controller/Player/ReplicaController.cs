namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 远端玩家插值驱动器。只消费权威快照做渲染插值，不预测。
    /// </summary>
    public class ReplicaController : EntityControllerBase
    {
        private NetEntitySyncRoot _syncRoot;

        #region 属性
        public override EntityDriveMode DriveMode => EntityDriveMode.Replica;
        #endregion

        public void Init(BaseEntity entity, NetEntitySyncRoot syncRoot)
        {
            Bind(entity);
            _syncRoot = syncRoot;
        }

        /// <summary>
        /// 添加快照
        /// </summary>
        public void AddSnapshot(in NetPositionSnapshot snapshot)
        {
            if (Target == null || _syncRoot == null || !_syncRoot.IsReplica) return;
            if (_syncRoot.IsInitialized && snapshot.EntityId != _syncRoot.EntityId) return;
            if (!TryGetPositionSync(out NetPositionSync positionSync)) return;

            positionSync.OnAuthoritySnapshot(snapshot);
        }

        private bool TryGetPositionSync(out NetPositionSync positionSync)
        {
            positionSync = null;
            return _syncRoot != null && _syncRoot.TryGetModule(ModuleType.Position, out positionSync);
        }

        private void Awake()
        {
            _syncRoot = GetComponent<NetEntitySyncRoot>();
        }
    }
}
