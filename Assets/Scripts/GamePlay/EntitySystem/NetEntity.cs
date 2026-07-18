using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体基类
    /// </summary>
    public class NetEntity<T> : MonoBehaviour, INetEntity<T> where T : struct, IEntitySnapshot
    {
        public uint Index { get; set; }
        public Vector3 LogicPosition { get; set; }
        public Vector3 LogicRotation { get; set; }

        protected Transform _transform;
        private float _tickDuration;
        private float _tickAccumulator;

        // ===== 快照间插值缓冲 =====
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private bool _hasSnapshot;
        // ===== 快照间插值缓冲 =====

        #region 网络同步

        public virtual void SetSnapshot(T snapshot)
        {
            if (_hasSnapshot)
            {
                // 保存当前逻辑位置作为插值起点
                _prevPosition = LogicPosition;
                _prevRotation = Quaternion.Euler(LogicRotation);
            }
            else
            {
                // 首个快照直接跳变
                _prevPosition = snapshot.Position;
                _prevRotation = Quaternion.Euler(snapshot.Rotation);
                _transform.position = _prevPosition;
                _transform.rotation = _prevRotation;
                _hasSnapshot = true;
            }

            LogicPosition = snapshot.Position;
            LogicRotation = snapshot.Rotation;
            _tickAccumulator = 0;
        }

        public virtual T GetSnapshot()
        {
            return new T()
            {
                Position = LogicPosition,
                Rotation = LogicRotation
            };
        }

        /// <summary>
        /// 快照间插值：从上一逻辑状态平滑过渡到当前逻辑状态
        /// </summary>
        protected virtual void Interpolation(float deltaTime)
        {
            if (!_hasSnapshot) return;

            _tickAccumulator += deltaTime;
            float t = Mathf.Clamp01(_tickAccumulator / _tickDuration);

            _transform.position = Vector3.Lerp(_prevPosition, LogicPosition, t);
            _transform.rotation = Quaternion.Slerp(_prevRotation, Quaternion.Euler(LogicRotation), t);
        }
        
        #endregion

        #region 生命周期

        protected virtual void Awake()
        {
            _transform = GetComponent<Transform>();

            // TODO: 服务器可能和客户端同步配置不同，后续通过外部传入同步属性
            SyncConfig config = SyncConfig.Instance;
            _tickDuration = 1f / config.snapShotTickRate;
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            
            Interpolation(deltaTime);
        }

        #endregion
    }
}