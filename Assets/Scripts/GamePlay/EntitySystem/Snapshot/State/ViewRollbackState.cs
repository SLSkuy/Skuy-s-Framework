using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    [Serializable]
    public struct ViewRollbackState
    {
        public float yaw;
        public float pitch;
        public Vector3 angularVelocity;
    }
}
