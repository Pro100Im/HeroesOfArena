using Unity.Entities;
using Unity.NetCode;

//namespace Unity.Template.CompetitiveActionMultiplayer
//{
    [GhostComponent]
    public struct OwningPlayer : IComponentData
    {
        [GhostField]
        public Entity Entity;
    }
//}
