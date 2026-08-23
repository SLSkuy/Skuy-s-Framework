using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct EntitySimulationState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public uint LocomotionState;
        public bool IsGrounded;

        public bool IsFinite()
        {
            return IsFinite(Position) && IsFinite(Rotation) && IsFinite(LinearVelocity) && IsFinite(AngularVelocity);
        }

        private static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) && value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.000001f;
    }
}
