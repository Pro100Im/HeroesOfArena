using Unity.Entities;
using Unity.NetCode;

namespace Game.Client
{
    [GhostComponent]
    public struct OwningPlayer : IComponentData
    {
        [GhostField]
        public Entity Entity;
    }
}
