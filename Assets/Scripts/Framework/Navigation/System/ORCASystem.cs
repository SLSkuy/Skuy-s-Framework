using Unity.Entities;

namespace Framework
{
    [UpdateAfter(typeof(PreferVelocitySystem))]
    public partial struct ORCASystem : ISystem
    {
        
    }
}