using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体根节点世界空间旋转能力，统一持有旋转模拟状态。
    /// </summary>
    public sealed class RotationModule : EntityModuleBase
    {
        private EntityConfig _config;
        private Vector3 _angularVelocity;

        #region 属性
        public override ModuleType ModuleType => ModuleType.Rotation;
        public Quaternion Rotation => transform.rotation;
        public Vector3 AngularVelocity => _angularVelocity;
        #endregion

        /// <summary>
        /// 初始化旋转配置。
        /// </summary>
        public void Init(EntityConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 根据瞄准或移动意图推进世界空间旋转。
        /// </summary>
        public void Rotate(Vector2 move, Vector2 aim, float deltaTime)
        {
            if (_config == null || deltaTime <= 0f) return;

            Quaternion previousRotation = transform.rotation;
            Vector3 planarDirection = aim.sqrMagnitude > Mathf.Epsilon ?
                new Vector3(aim.x, 0f, aim.y) :
                new Vector3(move.x, 0f, move.y);
            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                _angularVelocity = Vector3.zero;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                previousRotation,
                targetRotation,
                _config.meshTurnSpeed * deltaTime);

            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            _angularVelocity = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized * (angle / deltaTime) : Vector3.zero;
        }

        /// <summary>
        /// 直接应用世界空间旋转与角速度。
        /// </summary>
        public void Restore(Quaternion rotation, Vector3 angularVelocity)
        {
            transform.rotation = Normalize(rotation);
            _angularVelocity = angularVelocity;
        }

        private static Quaternion Normalize(Quaternion rotation)
        {
            float length = Mathf.Sqrt(
                rotation.x * rotation.x + rotation.y * rotation.y +
                rotation.z * rotation.z + rotation.w * rotation.w);
            if (length <= Mathf.Epsilon) return Quaternion.identity;

            float inverse = 1f / length;
            return new Quaternion(
                rotation.x * inverse,
                rotation.y * inverse,
                rotation.z * inverse,
                rotation.w * inverse);
        }
    }
}
