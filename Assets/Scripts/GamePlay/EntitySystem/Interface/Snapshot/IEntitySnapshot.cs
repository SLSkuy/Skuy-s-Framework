using UnityEngine;

namespace GamePlay.EntitySystem
{
    public interface IEntitySnapshot
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }
}