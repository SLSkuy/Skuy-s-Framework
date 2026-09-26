using UnityEngine;

namespace GamePlay.EntityFactory
{
    /// <summary>
    /// 销毁实体指令
    /// </summary>
    public struct DestroyEntityCommand
    {
        public GameObject instance;
        
        public DestroyEntityCommand(GameObject obj)
        {
            instance = obj;
        }
    }
}