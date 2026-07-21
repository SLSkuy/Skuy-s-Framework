using System.Collections.Generic;
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
        public NetEntityRole Role { get; private set; } = NetEntityRole.Authority;
        
        #region 逻辑状态
        public Vector3 LogicPosition { get; set; }
        public Vector3 LogicRotation { get; set; }
        #endregion

        private readonly List<T> _snapshotBuffer = new();
        private int _snapshotConsumeTickRate;
        
        private int _interpolationDelayTicks;
        private float _renderTick;
        private bool _hasRenderTick;
        
        public virtual void AddSnapshot(T snapshot)
        {
            // 权威模拟不使用快照进行状态更新
            if (Role != NetEntityRole.Replica) return;
            
            // 根据Tick顺序，插入新的状态
            // 倒叙遍历，找到第一个小于当前快照Tick序列的位置进行插入
            int insertIndex = _snapshotBuffer.Count;
            for (int i = _snapshotBuffer.Count - 1; i >= 0; i--)
            {
                // 序列重复，更新重复接收的状态
                if (_snapshotBuffer[i].SnapshotTick == snapshot.SnapshotTick)
                {
                    _snapshotBuffer[i] = snapshot;
                    return;
                }
                
                if (_snapshotBuffer[i].SnapshotTick < snapshot.SnapshotTick)
                {
                    insertIndex = i + 1;
                    break;
                }

                insertIndex = i;
            }
            _snapshotBuffer.Insert(insertIndex, snapshot);
            
            // 首次接收到快照，此时还未进行旋转缓冲，直接应用第一次快照作为初始状态
            if (!_hasRenderTick)
            {
                _renderTick = snapshot.SnapshotTick;
                _hasRenderTick = true;
                ApplySnapshot(snapshot);
            }
        }
        
        /// <summary>
        /// 强制应用当前快照状态
        /// </summary>
        protected virtual void ApplySnapshot(T snapshot)
        {
            LogicPosition = snapshot.Position;
            LogicRotation = snapshot.Rotation;
            transform.SetPositionAndRotation(snapshot.Position, Quaternion.Euler(snapshot.Rotation));
        }

        public virtual T GetSnapshot()
        {
            LogicPosition = transform.position;
            LogicRotation = transform.eulerAngles;
            return new T
            {
                Position = LogicPosition,
                Rotation = LogicRotation
            };
        }

        /// <summary>
        /// 设置网络同步类型
        /// </summary>
        public virtual void SetRole(NetEntityRole role)
        {
            if (Role == role) return;

            Role = role;
            _snapshotBuffer.Clear();
            _hasRenderTick = false;
            _renderTick = 0f;
        }

        /// <summary>
        /// 渲染插值，若有新的状态需由子类重写添加新的状态插值逻辑
        /// </summary>
        protected virtual void Interpolation(float deltaTime)
        {
            // 若缓存的快照数量少于配置值，不进行插值操作
            // 防止直接进行消耗快照插值结束后新的快照还未抵达，导致实体直接停止
            if (Role != NetEntityRole.Replica || _snapshotBuffer.Count < _interpolationDelayTicks)
            {
                return;
            }

            // 使用浮点 Tick 表示连续的客户端渲染时间
            // 渲染newest.Tick之前若干Tick的历史状态
            // 从而为网络抖动保留插值缓冲
            T newest = _snapshotBuffer[^1];
            float targetRenderTick = Mathf.Max(_snapshotBuffer[0].SnapshotTick, newest.SnapshotTick - _interpolationDelayTicks);
            
            // 将本帧经过的秒数转换成逻辑 Tick，并保证不超过消费缓存的Tick状态，为网络抖动保留插值缓冲
            _renderTick = Mathf.Min(_renderTick + deltaTime * _snapshotConsumeTickRate, targetRenderTick);

            // 丢弃已经完整播放过的快照
            // 循环结束后，通常满足：from.Tick <= renderTick < to.Tick
            while (_snapshotBuffer.Count >= 2 && _snapshotBuffer[1].SnapshotTick <= _renderTick)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            // 找到渲染时间两侧的快照，并计算其间的归一化插值比例。
            T from = _snapshotBuffer[0];
            T to = _snapshotBuffer[1];
            float tickSpan = Mathf.Max(1f, to.SnapshotTick - from.SnapshotTick);
            float t = Mathf.Clamp01((_renderTick - from.SnapshotTick) / tickSpan);

            // 插值应用位置和旋转
            LogicPosition = Vector3.Lerp(from.Position, to.Position, t);
            Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(from.Rotation), Quaternion.Euler(to.Rotation), t);
            LogicRotation = rotation.eulerAngles;
            transform.SetPositionAndRotation(LogicPosition, rotation);
        }

        #region 生命周期

        protected virtual void Awake()
        {
            SyncConfig config = SyncConfig.Instance;
            
            _snapshotConsumeTickRate = Mathf.Max(1, config.snapshotTickRate);
            _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);
            
            // 初始化逻辑位置，后续由主机同步
            LogicPosition = transform.position;
            LogicRotation = transform.eulerAngles;
        }

        protected virtual void Update()
        {
            Interpolation(Time.deltaTime);
        }

        #endregion
    }
}
