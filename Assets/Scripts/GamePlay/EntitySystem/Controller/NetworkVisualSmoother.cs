using UnityEngine;

namespace GamePlay.EntitySystem
{
    public sealed class NetworkVisualSmoother : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField, Min(0.01f)] private float positionSharpness = 16f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 16f;
        [SerializeField, Min(0f)] private float minSmoothDistance = 0.01f;
        [SerializeField, Min(0f)] private float minSmoothAngle = 0.25f;
        [SerializeField, Min(0f)] private float maxSmoothDistance = 2f;

        /// <summary>
        /// 权威修正前的预测位置
        /// </summary>
        private Vector3 _defaultLocalPosition;
        private Quaternion _defaultLocalRotation;

        /// <summary>
        /// 权威修正后的视觉位置
        /// </summary>
        private Vector3 _capturedWorldPosition;
        private Quaternion _capturedWorldRotation;
        private bool _hasCapturedPose;
        private bool _isFollowing;

        public Transform VisualRoot => modelRoot ? modelRoot : transform;

        /// <summary>
        /// 将视觉节点从逻辑对象拆出，独立跟随逻辑对象的世界坐标。
        /// </summary>
        public void BeginFollowing()
        {
            if (!modelRoot || _isFollowing) return;

            modelRoot.SetParent(null, true);
            _isFollowing = true;
            enabled = true;
        }

        public void CaptureBeforeCorrection()
        {
            if (!modelRoot || _isFollowing) return;

            _capturedWorldPosition = modelRoot.position;
            _capturedWorldRotation = modelRoot.rotation;
            _hasCapturedPose = true;
        }

        /// <summary>
        /// 反向应用预测偏移到本地坐标，插值转换为本地坐标插值到默认坐标
        /// </summary>
        public void ApplyCorrectionOffset(in NetPlayerSnapshot predict, in NetPlayerSnapshot authority)
        {
            if (!modelRoot || _isFollowing) return;

            float correctionDistance = Vector3.Distance(predict.Position, authority.Position);
            Quaternion predictRotation = Quaternion.Euler(predict.Rotation);
            Quaternion authorityRotation = Quaternion.Euler(authority.Rotation);
            float correctionAngle = Quaternion.Angle(predictRotation, authorityRotation);
            if (correctionDistance < minSmoothDistance && correctionAngle < minSmoothAngle)
            {
                if (_hasCapturedPose)
                {
                    modelRoot.SetPositionAndRotation(_capturedWorldPosition, _capturedWorldRotation);
                    _hasCapturedPose = false;
                    enabled = true;
                }

                return;
            }

            if (maxSmoothDistance > 0f && correctionDistance > maxSmoothDistance)
            {
                _hasCapturedPose = false;
                SnapToDefaultPose();
                return;
            }

            // 反向应用预测偏移到模型本地坐标，这样模型只需插值到本地初始位置即可
            if (_hasCapturedPose)
            {
                modelRoot.SetPositionAndRotation(_capturedWorldPosition, _capturedWorldRotation);
                _hasCapturedPose = false;
            }
            else
            {
                Vector3 predictedModelPosition = predict.Position + predictRotation * modelRoot.localPosition;
                Quaternion predictedModelRotation = predictRotation * modelRoot.localRotation;
                modelRoot.SetPositionAndRotation(predictedModelPosition, predictedModelRotation);
            }

            enabled = true;
        }

        /// <summary>
        /// 直接应用快照，适用于差距特别大的情况
        /// </summary>
        private void SnapToDefaultPose()
        {
            if (!modelRoot) return;

            if (_isFollowing)
            {
                modelRoot.SetPositionAndRotation(transform.TransformPoint(_defaultLocalPosition), transform.rotation * _defaultLocalRotation);
                return;
            }

            modelRoot.localPosition = _defaultLocalPosition;
            modelRoot.localRotation = _defaultLocalRotation;
            enabled = false;
        }

        #region 生命周期

        private void Awake()
        {
            if (modelRoot == null) modelRoot = transform.Find("Model");
            if (modelRoot == null)
            {
                enabled = false;
                return;
            }

            _defaultLocalPosition = modelRoot.localPosition;
            _defaultLocalRotation = modelRoot.localRotation;
            enabled = false;
        }

        private void LateUpdate()
        {
            Vector3 targetWorldPosition = transform.TransformPoint(_defaultLocalPosition);
            Quaternion targetWorldRotation = transform.rotation * _defaultLocalRotation;
            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

            modelRoot.position = Vector3.Lerp(modelRoot.position, targetWorldPosition, positionT);
            modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, targetWorldRotation, rotationT);

            if (!_isFollowing && Vector3.SqrMagnitude(modelRoot.position - targetWorldPosition) <= 0.000001f &&
                Quaternion.Angle(modelRoot.rotation, targetWorldRotation) <= 0.05f)
            {
                SnapToDefaultPose();
            }
        }

        private void OnDestroy()
        {
            if (_isFollowing && modelRoot != null)
            {
                Destroy(modelRoot.gameObject);
            }
        }

        #endregion
    }
}
