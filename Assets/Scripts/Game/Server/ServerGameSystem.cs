namespace Game.Server
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.Transforms;
    using Random = Unity.Mathematics.Random;
    using Game.Common.Components;
    using Game.GameResources;
    using Game.Client;

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct ServerGameSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            //state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<GameplayMaps>();

            // Creates random singleton
            var randomSeed = (uint)DateTime.Now.Millisecond;
            var randomEntity = state.EntityManager.CreateEntity();
            var mapSingleton = state.EntityManager.CreateSingletonBuffer<GameplayMaps>();

            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });

            state.EntityManager.GetBuffer<GameplayMaps>(mapSingleton).Add(default); // Default entry for index 0 (the server NetworkId index).
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameplayMaps = SystemAPI.GetSingletonBuffer<GameplayMaps>();
            var gameplayMapsEntity = SystemAPI.GetSingletonEntity<GameplayMaps>();
            var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;

            RefreshGameplayMap(ref state, gameplayMaps, connectionEventsForTick);

            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;

            //if (SystemAPI.HasSingleton<DisableCharacterDynamicContacts>())
            //    state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DisableCharacterDynamicContacts>());

            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            HandleJoinRequests(ref state, gameplayMapsEntity, gameResources, ecb);
            HandleCharacters(ref state, gameResources, gameplayMapsEntity, ecb);
        }

        private void RefreshGameplayMap(ref SystemState state, DynamicBuffer<GameplayMaps> gameplayMaps, NativeArray<NetCodeConnectionEvent>.ReadOnly connectionEventsForTick)
        {
            foreach (var evt in connectionEventsForTick)
            {
                if (evt.State == ConnectionState.State.Connected)
                {
                    var lengthNeeded = evt.Id.Value + 1;

                    if (gameplayMaps.Length < lengthNeeded)
                        gameplayMaps.Resize(lengthNeeded, NativeArrayOptions.ClearMemory);

                    gameplayMaps.ElementAt(evt.Id.Value).ConnectionEntity = evt.ConnectionEntity;
                }

                if (evt.State == ConnectionState.State.Disconnected)
                    gameplayMaps.ElementAt(evt.Id.Value) = default;
            }

            // Entities created via ECB have temporary Entity IDs.
            // These are not updated correctly - for Dynamic Buffers - unless we use ECB.AppendToBuffer
            // but this is an index lookup. So patch them.
            for (var i = gameplayMaps.Length - 1; i >= 0; i--)
            {
                ref var map = ref gameplayMaps.ElementAt(i);

                if (map.RemapTo.Value == default)
                    break;

                ref var dest = ref gameplayMaps.ElementAt(map.RemapTo.Value);

                TryPatch(map.FirstPersonPlayersEntity, ref dest.FirstPersonPlayersEntity);
                TryPatch(map.CharacterControllerEntity, ref dest.CharacterControllerEntity);

                map = default;

                static void TryPatch(Entity possibleRemapValue, ref Entity destination)
                {
                    if (possibleRemapValue != Entity.Null)
                        destination = possibleRemapValue;
                }
            }
        }

        private void HandleJoinRequests(ref SystemState state, Entity gameplayMapsEntity, GameResources gameResources, EntityCommandBuffer ecb)
        {
            // Process join requests
            foreach (var (request, rpcReceive, entity) in
                     SystemAPI.Query<ClientJoinRequestRpc, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<NetworkId>(rpcReceive.SourceConnection) &&
                    !SystemAPI.HasComponent<NetworkStreamInGame>(rpcReceive.SourceConnection))
                {
                    var ownerNetworkId = SystemAPI.GetComponent<NetworkId>(rpcReceive.SourceConnection);

                    // Spawn player
                    var playerEntity = ecb.Instantiate(gameResources.PlayerGhost);

                    ecb.AppendToBuffer(gameplayMapsEntity, new GameplayMaps
                    {
                        RemapTo = ownerNetworkId,
                        FirstPersonPlayersEntity = playerEntity,
                    });
                    ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = ownerNetworkId.Value });
                    ecb.AppendToBuffer(rpcReceive.SourceConnection, new LinkedEntityGroup { Value = playerEntity });

                    if (!request.IsSpectator)
                    {
                        // Request to spawn character
                        Entity spawnCharacterRequestEntity = ecb.CreateEntity();
                        ecb.AddComponent(spawnCharacterRequestEntity,
                            new SpawnCharacter { ClientEntity = rpcReceive.SourceConnection, Delay = -1f });
                    }

                    // Remember player for connection
                    ecb.AddComponent(rpcReceive.SourceConnection, new JoinedClient { PlayerEntity = playerEntity });
                    // Stream in game
                    ecb.AddComponent(rpcReceive.SourceConnection, new NetworkStreamInGame());

                    state.EntityManager.GetName(gameResources.PlayerGhost, out var playerNameFs);

                    if (playerNameFs.IsEmpty)
                        playerNameFs = nameof(gameResources.PlayerGhost);
                }

                ecb.DestroyEntity(entity);
            }
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
                        //    consumedSpawnPoints.Set(spawnPointIndex, true);

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
