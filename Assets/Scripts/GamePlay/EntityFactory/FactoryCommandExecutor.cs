using Framework;
using UnityEngine;
using YooAsset;

namespace GamePlay.EntityFactory
{
    /// <summary>
    /// 工厂命令处理器，负责接收工厂指令
    /// 委托对应工厂完成实际的游戏对象生成
    /// </summary>
    public class FactoryCommandExecutor
    {
        public GameObject Execute(SpawnEntityCommand command)
        {
            InstantiateOptions options = new(true, command.position, command.rotation);
            GameObject instance = Global.Instantiate(command.resPath, options);
            
            return instance;
        }

        public void Execute(DestroyEntityCommand command)
        {
            Global.Release(command.instance);
        }
    }
}