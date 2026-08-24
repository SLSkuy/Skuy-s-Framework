using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct MovementRollbackState
    {
        public Vector3 lastMoveDirection;
        public Vector3 linearVelocity;
        public Vector3 dashDirection;
        public float locomotionSpeed; 
        public float verticalVelocity; 
        public float dashRemainingTime;
        public int jumpCount;
        public bool isDashing;
    }
}
