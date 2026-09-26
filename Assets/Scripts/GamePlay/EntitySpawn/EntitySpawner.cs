using Framework;
using GamePlay.EntityFactory;
using GamePlay.Simulation;
using UnityEngine;

namespace GamePlay.EntitySpawn
{
    public class EntitySpawner : SubSystemBase
    {
        private FactoryCommandExecutor _factoryCommandExecutor;
        
        #region 属性
        public override int Priority => 700;
        #endregion

        public GameObject CreatePlayer(uint entityId, uint playerId, EntityObjectRole role)
        {
            GameObject instance = _factoryCommandExecutor.Execute(new SpawnEntityCommand("Entity_NetPlayer"));
            EntityObjectIdentity identity = instance.GetComponent<EntityObjectIdentity>();
            identity.Init(entityId, playerId, role);

            return instance;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _factoryCommandExecutor = new FactoryCommandExecutor();
        }

        public override void Destroy()
        {
            _factoryCommandExecutor = null;
        }

        #endregion
    }
}