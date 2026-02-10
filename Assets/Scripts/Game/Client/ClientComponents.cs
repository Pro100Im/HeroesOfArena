using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Game.Client
{
    public struct ClientJoinRequestRpc : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
    }

    [GhostComponent]
    public struct OwningPlayer : IComponentData
    {
        [GhostField]
        public Entity Entity;
    }
}
