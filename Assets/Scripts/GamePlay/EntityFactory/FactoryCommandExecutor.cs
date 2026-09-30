using Framework;
using UnityEngine;
using YooAsset;

namespace GamePlay.EntityFactory
{
    /// <summary>
    /// 工厂命令执行器，按实体类型实例化或释放对象
    /// </summary>
    public class FactoryCommandExecutor
    {
        public GameObject Execute(SpawnEntityCommand command)
        {
            string location = FactoryConfig.GetLocation(command.entityTypeId);
            InstantiateOptions options = new(true, command.position, command.rotation);
            
            return Global.Instantiate(location, ResGroup.Prefab, options);
        }

        public void Execute(DestroyEntityCommand command)
        {
            Global.Release(command.instance);
        }
    }
}
