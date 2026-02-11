namespace Game.Server.Spawner
{
    using Game.Client;
    using Game.Common.Components;
    using Game.GameResources;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.Transforms;

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(GoInGameServerSystem))]
    [BurstCompile]
    public partial struct PlayerSpawner : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnCharacter>();
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<GameplayMaps>();
            state.RequireForUpdate<FixedRandom>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameplayMaps = SystemAPI.GetSingletonBuffer<GameplayMaps>();
            var gameplayMapsEntity = SystemAPI.GetSingletonEntity<GameplayMaps>();
            var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;

            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;

            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            HandleCharacters(ref state, gameResources, gameplayMapsEntity, ecb);
        }

        private void HandleCharacters(ref SystemState state, GameResources gameResources, Entity gameplayMapsEntity, EntityCommandBuffer ecb)
        {
            // Spawn character requests
            if (SystemAPI.QueryBuilder().WithAll<SpawnCharacter>().Build().CalculateEntityCount() > 0)
            {
                var spawnPointsQuery = SystemAPI.QueryBuilder().WithAll<SpawnPoint, LocalToWorld>().Build();
                var spawnPointLtWs = spawnPointsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
                var consumedSpawnPoints = new NativeBitArray(spawnPointLtWs.Length, Allocator.Temp);

                ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;

                foreach (var (spawnRequest, entity) in SystemAPI.Query<RefRW<SpawnCharacter>>().WithEntityAccess())
                {
                    if (spawnRequest.ValueRW.Delay > 0f)
                    {
                        spawnRequest.ValueRW.Delay -= SystemAPI.Time.DeltaTime;
                    }
                    else
                    {
                        if (SystemAPI.HasComponent<NetworkId>(spawnRequest.ValueRW.ClientEntity) &&
                            SystemAPI.HasComponent<JoinedClient>(spawnRequest.ValueRW.ClientEntity))
                        {
                            // Try to find a free (i.e. unblocked by other players) spawn point:
                            if (!TryFindSpawnPoint(gameResources, spawnPointLtWs, random, consumedSpawnPoints,
                                    out var spawnPoint))
                                break;

                            var ownerNetworkId = SystemAPI.GetComponent<NetworkId>(spawnRequest.ValueRW.ClientEntity);
                            var playerEntity = SystemAPI.GetComponent<JoinedClient>(spawnRequest.ValueRW.ClientEntity)
                                .PlayerEntity;
                            // Spawn character
                            var characterEntity = ecb.Instantiate(gameResources.CharacterGhost);

                            ecb.AppendToBuffer(gameplayMapsEntity, new GameplayMaps
                            {
                                RemapTo = ownerNetworkId,
                                CharacterControllerEntity = characterEntity,
                            });

                            ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = ownerNetworkId.Value });
                            ecb.SetComponent(characterEntity, LocalTransform.FromPositionRotation(spawnPoint.Position, spawnPoint.Rotation));
                            ecb.SetComponent(characterEntity, new OwningPlayer { Entity = playerEntity });

                            ecb.AppendToBuffer(spawnRequest.ValueRW.ClientEntity, new LinkedEntityGroup { Value = characterEntity });

                            state.EntityManager.GetName(gameResources.CharacterGhost, out var characterNameFs);

                            if (characterNameFs.IsEmpty)
                                characterNameFs = nameof(gameResources.CharacterGhost);
                        }

                        ecb.DestroyEntity(entity);
                    }
                }

                consumedSpawnPoints.Dispose();
                spawnPointLtWs.Dispose();
            }
        }

        private bool TryFindSpawnPoint(GameResources gameResources, NativeArray<LocalToWorld> spawnPointLtWs,
            FixedRandom random, NativeBitArray consumedSpawnPoints, out LocalToWorld spawnPoint)
        {
            spawnPoint = default;

            if (spawnPointLtWs.Length > 0)
            {
                //var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                var randSpawnPointIndex = random.Random.NextInt(0, spawnPointLtWs.Length - 1);

                for (var attempt = 0; attempt < spawnPointLtWs.Length; attempt++)
                {
                    var spawnPointIndex = (randSpawnPointIndex + attempt) % spawnPointLtWs.Length;

                    if (!consumedSpawnPoints.IsSet(spawnPointIndex))
                    {
                        //Debug.Assert(gameResources.SpawnPointCollisionFilter.CollidesWith != default);

                        //var spawnPointBlocked = collisionWorld.CheckSphere(
                        //    spawnPointLtWs[spawnPointIndex].Position,
                        //    gameResources.SpawnPointBlockRadius;

                        //if (!spawnPointBlocked)
                        //{
                        //    spawnPoint = spawnPointLtWs[spawnPointIndex];
                        consumedSpawnPoints.Set(spawnPointIndex, true);

                        //    return true;
                        //}

                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}