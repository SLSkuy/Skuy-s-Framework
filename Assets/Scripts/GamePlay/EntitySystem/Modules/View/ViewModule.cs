using UnityEngine;
using Utils;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 第三人称视角节点模拟：在实体根上查找 orientation，用瞄准增量驱动偏航与俯仰。
    /// </summary>
    public sealed class ViewModule : EntityModuleBase<ViewRollbackState>
    {
        private const string ORIENTATION_CHILD_NAME = "orientation";

        private EntityConfig _config;
        private Transform _orientation;

        private float _yaw;
        private float _pitch;

        #region 属性
        public override ModuleType ModuleType => ModuleType.View;
        public Quaternion ViewRotation => Quaternion.Euler(_pitch, _yaw, 0f);
        public Vector3 AngularVelocity { get; private set; }
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        #endregion

        /// <summary>
        /// 绑定配置并解析默认视角子节点
        /// </summary>
        public void Init(EntityConfig config)
        {
            _config = config;
            _orientation = transform.Find(ORIENTATION_CHILD_NAME);
            Vector3 euler = _orientation.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = MathUtils.NormalizePitch(euler.x);
        }

        /// <summary>
        /// 按瞄准增量推进视角，并写入 orientation 的世界旋转
        /// </summary>
        public void Look(Vector2 aimDelta, float deltaTime)
        {
            if (deltaTime <= 0f) return;

            Quaternion previousRotation = ViewRotation;
            _yaw += aimDelta.x * _config.aimHorizontalSpeed * deltaTime;
            _pitch -= aimDelta.y * _config.aimVerticalSpeed * deltaTime;
            _pitch = Mathf.Clamp(_pitch, _config.minAimPitch, _config.maxAimPitch);
            _orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            AngularVelocity = MathUtils.CalAngularVelocity(previousRotation, ViewRotation, deltaTime);
        }
        
        #region 快照逻辑
        
        /// <summary>
        /// 从网络可见四元数恢复视角，供 Replica 插值与权威应用使用
        /// </summary>
        public void Restore(Quaternion viewRotation, Vector3 angularVelocity)
        {
            Vector3 euler = viewRotation.eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.Clamp(MathUtils.NormalizePitch(euler.x), _config.minAimPitch, _config.maxAimPitch);

            AngularVelocity = angularVelocity;
            _orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        /// <summary>
        /// 捕获视角回滚状态
        /// </summary>
        public override ViewRollbackState CaptureRollbackState()
        {
            return new ViewRollbackState
            {
                viewAngularVelocity = AngularVelocity,
                viewRotation = ViewRotation,
                yaw = _yaw,
                pitch = _pitch
            };
        }

        /// <summary>
        /// 恢复视角回滚状态
        /// </summary>
        public override void RestoreRollbackState(in ViewRollbackState state)
        {
            _yaw = state.yaw;
            _pitch = Mathf.Clamp(state.pitch, _config.minAimPitch, _config.maxAimPitch);
            AngularVelocity = state.viewAngularVelocity;
            _orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        #endregion
    }
}
