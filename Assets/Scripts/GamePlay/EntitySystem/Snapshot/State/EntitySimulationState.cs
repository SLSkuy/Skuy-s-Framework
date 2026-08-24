using System;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体Tick模拟状态基础单位
    /// </summary>
    [Serializable]
    public struct EntitySimulationState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion viewRotation;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public uint locomotionState;
        public bool isGrounded;
    }
}
