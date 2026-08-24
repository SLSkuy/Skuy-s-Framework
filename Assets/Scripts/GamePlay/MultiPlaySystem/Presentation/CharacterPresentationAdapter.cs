using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 角色网络可见 Transform 捕获、权威应用和预测表现平滑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterPresentationAdapter : MonoBehaviour
    {
        [SerializeField] private Transform presentationRoot;
        [SerializeField, Min(0.001f)] private float correctionHalfLife = 0.08f;

        private Vector3 _presentationLocalPosition;
        private Quaternion _presentationLocalRotation;
        private Vector3 _preservedWorldPosition;
        private Quaternion _preservedWorldRotation;
        private bool _isSmoothingCorrection;
        private bool _hasPreservedPose;
        private bool _presentationOwnsBodyYaw;

        /// <summary>
        /// 捕获当前网络可见 Transform 状态。
        /// </summary>
        public EntitySimulationState CaptureState()
        {
            EntityCharacter entity = GetComponent<EntityCharacter>();
            if (entity != null && entity.IsInitialized) return entity.CaptureSimulationState();

            Transform mesh = transform.Find("mesh");
            return new EntitySimulationState
            {
                position = transform.position,
                rotation = mesh != null ? mesh.rotation : Quaternion.identity,
                viewRotation = Quaternion.identity,
                linearVelocity = Vector3.zero,
                angularVelocity = Vector3.zero
            };
        }

        /// <summary>
        /// 将网络 Transform 状态安全应用到当前对象。
        /// </summary>
        public void ApplyState(in EntitySimulationState state)
        {
            TransformModule transformModule = GetComponent<TransformModule>();
            if (transformModule != null)
            {
                transformModule.Teleport(state.position);
                transformModule.Restore(state.rotation, state.angularVelocity);
            }
            else
            {
                transform.position = state.position;
                transform.rotation = Quaternion.identity;
                Transform mesh = transform.Find("mesh");
                if (mesh != null) mesh.rotation = state.rotation;
            }

            ViewModule view = GetComponent<ViewModule>();
            if (view != null) view.Restore(state.viewRotation, Vector3.zero);
            else
            {
                Transform orientation = transform.Find("orientation");
                if (orientation != null) orientation.rotation = state.viewRotation;
            }
        }

        /// <summary>
        /// 记录表现根节点的本地姿态，供预测校正使用。
        /// mesh 承载模拟身体偏航时，校正不得把旋转拉回预制体本地值。
        /// </summary>
        public void ApplyRole(EntitySimulationMode mode)
        {
            if (presentationRoot == null) return;
            _presentationLocalPosition = presentationRoot.localPosition;
            _presentationLocalRotation = presentationRoot.localRotation;
            Transform mesh = transform.Find("mesh");
            _presentationOwnsBodyYaw = mesh != null && presentationRoot == mesh;
        }

        /// <summary>
        /// 保存校正前表现节点的世界空间姿态。
        /// </summary>
        public void BeginPredictionCorrection()
        {
            if (presentationRoot == null) return;
            Transform mesh = transform.Find("mesh");
            _presentationOwnsBodyYaw = mesh != null && presentationRoot == mesh;
            _preservedWorldPosition = presentationRoot.position;
            _preservedWorldRotation = presentationRoot.rotation;
            _hasPreservedPose = true;
        }

        /// <summary>
        /// 在模拟校正结束后恢复表现姿态，并逐帧收敛到模拟根节点。
        /// </summary>
        public void EndPredictionCorrection(bool smoothCorrection)
        {
            if (presentationRoot == null || !_hasPreservedPose) return;
            _hasPreservedPose = false;
            if (!smoothCorrection)
            {
                ResetPresentationPose();
                return;
            }

            if (_presentationOwnsBodyYaw)
            {
                presentationRoot.position = _preservedWorldPosition;
            }
            else
            {
                presentationRoot.SetPositionAndRotation(_preservedWorldPosition, _preservedWorldRotation);
            }

            _isSmoothingCorrection = true;
        }

        private void ResetPresentationPose()
        {
            _isSmoothingCorrection = false;
            _hasPreservedPose = false;
            if (presentationRoot == null) return;
            presentationRoot.localPosition = _presentationLocalPosition;
            if (!_presentationOwnsBodyYaw)
            {
                presentationRoot.localRotation = _presentationLocalRotation;
            }
        }

        #region Lifecycle

        private void LateUpdate()
        {
            if (!_isSmoothingCorrection || presentationRoot == null) return;

            float blend = 1f - Mathf.Pow(0.5f, Time.deltaTime / correctionHalfLife);
            presentationRoot.localPosition = Vector3.Lerp(
                presentationRoot.localPosition, _presentationLocalPosition, blend);

            if (_presentationOwnsBodyYaw)
            {
                if (Vector3.SqrMagnitude(presentationRoot.localPosition - _presentationLocalPosition) > 0.000001f)
                {
                    return;
                }

                ResetPresentationPose();
                return;
            }

            presentationRoot.localRotation = Quaternion.Slerp(
                presentationRoot.localRotation, _presentationLocalRotation, blend);

            if (Vector3.SqrMagnitude(presentationRoot.localPosition - _presentationLocalPosition) > 0.000001f ||
                Quaternion.Angle(presentationRoot.localRotation, _presentationLocalRotation) > 0.05f) return;
            ResetPresentationPose();
        }

        #endregion
    }
}
