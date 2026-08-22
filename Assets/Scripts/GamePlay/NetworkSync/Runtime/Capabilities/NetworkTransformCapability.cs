using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// Position/Rotation 状态捕获、权威应用和预测表现平滑能力。
    /// </summary>
    public sealed class NetworkTransformCapability : NetworkObjectCapabilityBase
    {
        [SerializeField] private Transform presentationRoot;
        [SerializeField, Min(0.001f)] private float correctionHalfLife = 0.08f;

        private Vector3 _presentationLocalPosition;
        private Quaternion _presentationLocalRotation;
        private Vector3 _preservedWorldPosition;
        private Quaternion _preservedWorldRotation;
        private bool _isSmoothingCorrection;
        private bool _hasPreservedPose;

        #region 属性
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Transform;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
        #endregion

        /// <inheritdoc />
        public override bool SupportsMode(EntitySimulationMode mode) => true;

        /// <summary>
        /// 捕获当前网络可见 Transform 状态。
        /// </summary>
        public EntitySimulationState CaptureState()
        {
            EntitySimulationObject entity = GetComponent<EntitySimulationObject>();
            if (entity != null && entity.IsInitialized) return entity.CaptureSimulationState();

            return new EntitySimulationState
            {
                Position = transform.position,
                Rotation = transform.rotation,
                LinearVelocity = Vector3.zero,
                AngularVelocity = Vector3.zero
            };
        }

        /// <summary>
        /// 将网络 Transform 状态安全应用到当前对象。
        /// </summary>
        public void ApplyState(in EntitySimulationState state)
        {
            MovementModule movement = GetComponent<MovementModule>();
            if (movement != null) movement.Teleport(state.Position);
            else transform.position = state.Position;

            RotationModule rotation = GetComponent<RotationModule>();
            if (rotation != null) rotation.Restore(state.Rotation, state.AngularVelocity);
            else transform.rotation = state.Rotation;
        }

        /// <summary>
        /// 保存校正前表现节点的世界空间姿态。
        /// </summary>
        public void BeginPredictionCorrection()
        {
            if (presentationRoot == null) return;
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

            presentationRoot.SetPositionAndRotation(_preservedWorldPosition, _preservedWorldRotation);
            _isSmoothingCorrection = true;
        }

        protected override void OnActivated(EntitySimulationMode mode)
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = mode != EntitySimulationMode.Replica;
            if (presentationRoot == null) return;
            _presentationLocalPosition = presentationRoot.localPosition;
            _presentationLocalRotation = presentationRoot.localRotation;
        }

        protected override void OnDeactivated()
        {
            ResetPresentationPose();
        }

        private void ResetPresentationPose()
        {
            _isSmoothingCorrection = false;
            _hasPreservedPose = false;
            if (presentationRoot == null) return;
            presentationRoot.SetLocalPositionAndRotation(_presentationLocalPosition, _presentationLocalRotation);
        }

        #region 生命周期

        private void LateUpdate()
        {
            if (!_isSmoothingCorrection || presentationRoot == null) return;

            float blend = 1f - Mathf.Pow(0.5f, Time.deltaTime / correctionHalfLife);
            presentationRoot.localPosition = Vector3.Lerp(
                presentationRoot.localPosition, _presentationLocalPosition, blend);
            presentationRoot.localRotation = Quaternion.Slerp(
                presentationRoot.localRotation, _presentationLocalRotation, blend);

            if (Vector3.SqrMagnitude(presentationRoot.localPosition - _presentationLocalPosition) > 0.000001f ||
                Quaternion.Angle(presentationRoot.localRotation, _presentationLocalRotation) > 0.05f) return;
            ResetPresentationPose();
        }

        #endregion
    }
}
