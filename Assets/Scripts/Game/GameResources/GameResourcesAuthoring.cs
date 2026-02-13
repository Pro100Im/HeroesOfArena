using Unity.Entities;
using Unity.Physics;
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

        [Tooltip("Prevent player spawning if another player is within this radius!")]
        public float SpawnPointBlockRadius = 1f;
        public LayerMask PlayerLayerMask;

        public class Baker : Baker<GameResourcesAuthoring>
        {
            public override void Bake(GameResourcesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var mask = (uint)authoring.PlayerLayerMask.value;
                var filter = CollisionFilter.Default;

                filter.CollidesWith = mask;

                AddComponent(entity, new GameResources
                {
                    DespawnTicks = authoring.DespawnTicks,
                    PolledEventsTicks = authoring.PolledEventsTicks,
                    RespawnTime = authoring.RespawnTimeSeconds,
                    SpawnPointBlockRadius = authoring.SpawnPointBlockRadius,

                    PlayerGhost = GetEntity(authoring.PlayerGhost, TransformUsageFlags.Dynamic),
                    CharacterGhost = GetEntity(authoring.CharacterGhost, TransformUsageFlags.Dynamic),

                    SpawnPointCollisionFilter = filter
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

        public float SpawnPointBlockRadius;
        public CollisionFilter SpawnPointCollisionFilter;
    }
}