using System;
using Events;
using Framework;
using NetSync;
using Network;
using Utils;

namespace GamePlay.BattleSystem
{
    public class BattleEntityClientHandler
    {
        private readonly BattleEntityManager _entityManager;
        private NetClient _client;

        public BattleEntityClientHandler(BattleEntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        #region 消息绑定

        public void Bind()
        {
            Unbind();
            _client = Global.Get<NetClient>();
            _client.RegisterHandler<Entity_Spawn_Notify>(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, HandleEntitySpawnNotify);
            _client.RegisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
        }

        public void Unbind()
        {
            if (_client == null) return;
            
            _client.UnregisterHandler<Entity_Spawn_Notify>(NetEvent.GAME_ENTITY_SPAWN_NOTIFY, HandleEntitySpawnNotify);
            _client.UnregisterHandler<Entity_Destroy_Notify>(NetEvent.GAME_ENTITY_DESTROY_NOTIFY, HandleEntityDestroyNotify);
            _client = null;
        }

        #endregion
        
        #region 接收消息
        
        private void HandleEntitySpawnNotify(Entity_Spawn_Notify notify)
        {
            _entityManager.HandleSpawnNotify(notify.EntityId, Guid.Parse(notify.PlayerId), notify.ToCommand(), notify.Snapshot.ToSnapshot());
        }

        private void HandleEntityDestroyNotify(Entity_Destroy_Notify notify)
        {
            _entityManager.HandleDestroyNotify(notify.ToCommand());
        }
        
        #endregion
    }
}