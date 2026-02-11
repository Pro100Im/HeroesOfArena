namespace Game.Server
{
    using Game.Client;
    using Game.Common.Components;
    using Game.GameResources;
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Random = Unity.Mathematics.Random;

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct GoInGameServerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<GameplayMaps>();

            var mapSingleton = state.EntityManager.CreateSingletonBuffer<GameplayMaps>();
            state.EntityManager.GetBuffer<GameplayMaps>(mapSingleton).Add(default); // Default entry for index 0 (the server NetworkId index).

            var randomSeed = (uint)DateTime.Now.Millisecond;
            var randomEntity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameplayMaps = SystemAPI.GetSingletonBuffer<GameplayMaps>();
            var gameplayMapsEntity = SystemAPI.GetSingletonEntity<GameplayMaps>();
            var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;

            RefreshGameplayMap(ref state, gameplayMaps, connectionEventsForTick);

            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;

            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            HandleJoinRequests(ref state, gameplayMapsEntity, gameResources, ecb);
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

                TryPatch(map.CharacterPlayersEntity, ref dest.CharacterPlayersEntity);
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
                        CharacterPlayersEntity = playerEntity,
                    });
                    ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = ownerNetworkId.Value });
                    ecb.AppendToBuffer(rpcReceive.SourceConnection, new LinkedEntityGroup { Value = playerEntity });

                    // Request to spawn character
                    var spawnCharacterRequestEntity = ecb.CreateEntity();

                    ecb.AddComponent(spawnCharacterRequestEntity, new SpawnCharacter { ClientEntity = rpcReceive.SourceConnection, Delay = -1f });
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
    }
}
