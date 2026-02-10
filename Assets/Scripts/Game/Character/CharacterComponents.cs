using Unity.Entities;

namespace Game.Character
{
    public struct Character : IComponentData
    {
        //public Entity ViewEntity;
    }

    public struct CharacterInitialized : IComponentData, IEnableableComponent
    {

    }
}
