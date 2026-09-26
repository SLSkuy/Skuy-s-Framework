using UnityEngine;

namespace GamePlay.EntityFactory
{
    /// <summary>
    /// 生成实体命令
    /// </summary>
    public struct SpawnEntityCommand
    {
        public string resPath;
        public Vector3 position;
        public Quaternion rotation;

        public SpawnEntityCommand(string path)
        {
            resPath = path;
            position = default;
            rotation = default;
        }

        public SpawnEntityCommand(string path, Vector3 pos, Quaternion rot)
        {
            resPath = path;
            position = pos;
            rotation = rot;
        }
    }
}