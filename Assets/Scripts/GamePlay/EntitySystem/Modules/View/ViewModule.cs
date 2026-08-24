using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 第三人称视角节点模拟：在实体根上查找 orientation，用瞄准增量驱动偏航与俯仰。
    /// </summary>
    public sealed class ViewModule : EntityModuleBase
    {
        private const string OrientationChildName = "orientation";

        private EntityConfig _config;
        private Transform _orientation;
        private Vector3 _angularVelocity;
        private float _yaw;
        private float _pitch;

        #region 属性
        public override ModuleType ModuleType => ModuleType.View;
        public Quaternion ViewRotation => Quaternion.Euler(_pitch, _yaw, 0f);
        public Vector3 AngularVelocity => _angularVelocity;
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        #endregion

        /// <summary>
        /// 绑定配置并解析默认视角子节点。
        /// </summary>
        public void Init(EntityConfig config)
        {
            _config = config;
            _orientation = transform.Find(OrientationChildName);
            if (_orientation == null)
            {
                Debug.LogError($"{name} 缺少名为 {OrientationChildName} 的直接子节点，ViewModule 无法写入视角。", this);
                return;
            }

            Vector3 euler = _orientation.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizePitch(euler.x);
        }

        /// <summary>
        /// 按瞄准增量推进视角，并写入 orientation 的世界旋转。
        /// </summary>
        public void Look(Vector2 aimDelta, float deltaTime)
        {
            if (_config == null || deltaTime <= 0f) return;

            Quaternion previousRotation = ViewRotation;
            _yaw += aimDelta.x * _config.aimHorizontalSpeed * deltaTime;
            _pitch -= aimDelta.y * _config.aimVerticalSpeed * deltaTime;
            _pitch = Mathf.Clamp(_pitch, _config.minAimPitch, _config.maxAimPitch);
            ApplyOrientationRotation();
            _angularVelocity = ToAngularVelocity(previousRotation, ViewRotation, deltaTime);
        }

        /// <summary>
        /// 捕获视角回滚状态。
        /// </summary>
        public ViewRollbackState CaptureRollbackState()
        {
            return new ViewRollbackState
            {
                yaw = _yaw,
                pitch = _pitch,
                angularVelocity = _angularVelocity
            };
        }

        /// <summary>
        /// 恢复视角回滚状态。
        /// </summary>
        public void RestoreRollbackState(in ViewRollbackState state)
        {
            _yaw = state.yaw;
            _pitch = Mathf.Clamp(state.pitch, _config != null ? _config.minAimPitch : state.pitch,
                _config != null ? _config.maxAimPitch : state.pitch);
            _angularVelocity = state.angularVelocity;
            ApplyOrientationRotation();
        }

        /// <summary>
        /// 将 yaw/pitch 写回 orientation 世界旋转。根节点保持单位旋转，mesh 转向不会带动视角。
        /// </summary>
        public void ApplyWorldPose()
        {
            ApplyOrientationRotation();
        }

        /// <summary>
        /// 从网络可见四元数恢复视角，供 Replica 插值与权威应用使用。
        /// </summary>
        public void Restore(Quaternion viewRotation, Vector3 angularVelocity)
        {
            Vector3 euler = viewRotation.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizePitch(euler.x);
            if (_config != null)
            {
                _pitch = Mathf.Clamp(_pitch, _config.minAimPitch, _config.maxAimPitch);
            }

            _angularVelocity = angularVelocity;
            ApplyOrientationRotation();
        }

        private void ApplyOrientationRotation()
        {
            if (_orientation == null) return;
            _orientation.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private static float NormalizePitch(float eulerX)
        {
            if (eulerX > 180f) eulerX -= 360f;
            return eulerX;
        }

        private static Vector3 ToAngularVelocity(Quaternion previousRotation, Quaternion currentRotation, float deltaTime)
        {
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            return axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized * (angle / deltaTime) : Vector3.zero;
        }
    }
}
