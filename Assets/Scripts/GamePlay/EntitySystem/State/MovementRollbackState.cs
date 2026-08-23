using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct MovementRollbackState
    {
        public Vector3 LastMoveDirection;
        public Vector3 LinearVelocity;
        public Vector3 DashDirection;
        public float LocomotionSpeed;
        public float VerticalVelocity;
        public float DashRemainingTime;
        public int JumpCount;
        public bool IsDashing;
    }
}
