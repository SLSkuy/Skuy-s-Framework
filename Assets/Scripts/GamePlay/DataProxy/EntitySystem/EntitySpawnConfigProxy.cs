using System;
using System.Collections.Generic;
using Framework;
using GamePlay.EntitySpawn;

namespace GamePlay.DataProxy
{
    public class EntitySpawnConfigProxy : BaseDataProxy
    {
        public override string DataPath => "EntityType";
        public override bool IsNeedSaveToLocal => false;

        private readonly Dictionary<EntityType, string> _locations = new()
        {
            { EntityType.Player , "Entity_NetPlayer" },
        };

        #region 属性
        /// <summary>
        /// 每帧最大执行的实体生成数量，强制分帧执行实体生成逻辑
        /// </summary>
        public int MaxExecutePerFrame => 10;
        #endregion

        public string GetEntityLocation(EntityType entityType)
        {
            if (_locations.TryGetValue(entityType, out var location))
            {
                return location;
            }
            
            throw new ArgumentException($"[{GetType().Name}] Unknown entity type {entityType}");
        }
        
        #region 数据管理方法

        public override void Load()
        {
            // TODO: 后续接入配表读取数据
        }
        
        #endregion
    }
}