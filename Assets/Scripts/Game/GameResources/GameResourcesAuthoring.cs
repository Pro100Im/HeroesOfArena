using Unity.Entities;
using UnityEngine;

namespace Game.GameResources
{
    public class GameResourcesAuthoring : MonoBehaviour
    {
        [Header("Network Parameters")]
        public uint DespawnTicks = 30;
        public uint PolledEventsTicks = 30;

        [Header("General Parameters")]
        public float RespawnTimeSeconds = 4f;

        [Header("Ghost Prefabs")]
        public GameObject PlayerGhost;
        public GameObject CharacterGhost;

        [Header("Other Prefabs")]
        public GameObject SpectatorPrefab;

        [Tooltip("Prevent player spawning if another player is within this radius!")]
        public float SpawnPointBlockRadius = 1f;

        public class Baker : Baker<GameResourcesAuthoring>
        {
            public override void Bake(GameResourcesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GameResources
                {
                    DespawnTicks = authoring.DespawnTicks,
                    PolledEventsTicks = authoring.PolledEventsTicks,
                    RespawnTime = authoring.RespawnTimeSeconds,
                    SpawnPointBlockRadius = authoring.SpawnPointBlockRadius,

                    PlayerGhost = GetEntity(authoring.PlayerGhost, TransformUsageFlags.Dynamic),
                    CharacterGhost = GetEntity(authoring.CharacterGhost, TransformUsageFlags.Dynamic),
                    SpectatorPrefab = GetEntity(authoring.SpectatorPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

    public struct GameResources : IComponentData
    {
        public uint DespawnTicks;
        public uint PolledEventsTicks;
        public float RespawnTime;

        public Entity PlayerGhost;
        public Entity CharacterGhost;
        public Entity SpectatorPrefab;

        public float SpawnPointBlockRadius;
    }
}