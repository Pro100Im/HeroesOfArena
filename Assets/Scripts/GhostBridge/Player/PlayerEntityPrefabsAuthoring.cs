using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class PlayerEntityPrefabsAuthoring : MonoBehaviour
{
    [field: SerializeField] public GhostAuthoringComponent ClientInputEntityPrefab { get; private set; }
    [field: SerializeField] public GhostAuthoringComponent PlayerEntityPrefab { get; private set; }
}

public struct PlayerEntityPrefabs : IComponentData
{
    public Entity ClientInputEntityPrefab;
    public Entity PlayerEntityPrefab;
}

public class PlayerEntityPrefabsBaker : Baker<PlayerEntityPrefabsAuthoring>
{
    public override void Bake(PlayerEntityPrefabsAuthoring authoring)
    {
        if (authoring.ClientInputEntityPrefab == null)
        {
            return;
        }
        
        var playerPrefabsEntity = GetEntity(TransformUsageFlags.None);

        AddComponent(playerPrefabsEntity, new PlayerEntityPrefabs
        {
            ClientInputEntityPrefab = GetEntity(authoring.ClientInputEntityPrefab.gameObject, TransformUsageFlags.None),
            PlayerEntityPrefab = 
                authoring.PlayerEntityPrefab != null ?
                    GetEntity(authoring.PlayerEntityPrefab.gameObject, TransformUsageFlags.None) 
                    : Entity.Null
        });
    }
}
