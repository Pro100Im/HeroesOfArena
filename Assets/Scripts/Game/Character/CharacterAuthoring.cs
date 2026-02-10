using Game.Client;
using Unity.Entities;
using UnityEngine;

namespace Game.Character
{
    public class CharacterAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Character());
                AddComponent(entity, new CharacterInitialized());
                AddComponent(entity, new OwningPlayer());
                SetComponentEnabled<CharacterInitialized>(entity, false);
            }
        }
    }
}
