using UnityEngine;

namespace GamePlay.EntitySystem
{
    public struct NetPlayerSnapshot : IPlayerSnapshot
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}