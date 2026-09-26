using System;

namespace GamePlay.EntityFactory
{
    public static class FactoryConfig
    {
        private const string PLAYER_LOCATION = "Entity_NetPlayer";

        public static string GetLocation(EntityType entityTypeId)
        {
            if (entityTypeId == EntityType.Player) return PLAYER_LOCATION;
            
            throw new ArgumentException($"Unknown entity type {entityTypeId}");
        }
    }
}