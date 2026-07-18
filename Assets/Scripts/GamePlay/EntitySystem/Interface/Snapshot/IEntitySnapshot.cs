using UnityEngine;

namespace GamePlay.EntitySystem
{
    public interface IEntitySnapshot
    {
        uint Tick { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}
