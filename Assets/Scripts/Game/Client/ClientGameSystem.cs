namespace Game.Client
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.Transforms;
    using UnityEngine;
    using Random = Unity.Mathematics.Random;
    using Game.GameResources;
    using Game.Common.Components;
    using Game.Character;

    public struct ClientJoinRequestRpc : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
        public bool IsSpectator;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientGameSystem : ISystem
    {
        EntityQuery m_SpectatorSpawnPointsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();

            var randomSeed = (uint)DateTime.Now.Millisecond;
            var randomEntity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });

            m_SpectatorSpawnPointsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpectatorSpawnPoint, LocalToWorld>().Build(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameResources = SystemAPI.GetSingleton<GameResources>();

            HandleSendJoinRequest(ref state, gameResources);
            HandleCharacterSetup(ref state);
        }

        private void HandleSendJoinRequest(ref SystemState state, GameResources gameResources)
        {
            if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var clientEntity)
                || SystemAPI.HasComponent<NetworkStreamInGame>(clientEntity))
                return;

            var joinRequestEntity = state.EntityManager.CreateEntity(ComponentType.ReadOnly<ClientJoinRequestRpc>(),
                ComponentType.ReadWrite<SendRpcCommandRequest>());
            var playerName = GameSettings.Instance.PlayerName;

            if (state.WorldUnmanaged.IsThinClient()) // Random names for thin clients.
            {
                ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                playerName = $"[Bot {random.Random.NextInt(1, 99):00}] {playerName}";
            }

            var clientJoinRequestRpc = new ClientJoinRequestRpc();

            clientJoinRequestRpc.IsSpectator = GameSettings.Instance.SpectatorToggle;
            clientJoinRequestRpc.PlayerName.CopyFromTruncated(playerName); // Prevents exceptions on long strings.

            state.EntityManager.SetComponentData(joinRequestEntity, clientJoinRequestRpc);
            state.EntityManager.AddComponentData(clientEntity, new NetworkStreamInGame());

            // Spectator mode
            if (GameSettings.Instance.SpectatorToggle)
            {
                LocalToWorld spawnPoint = default;

                using var spectatorSpawnPoints = m_SpectatorSpawnPointsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

                if (spectatorSpawnPoints.Length > 0)
                {
                    ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                    spawnPoint = spectatorSpawnPoints[random.Random.NextInt(0, spectatorSpawnPoints.Length - 1)];
                }

                var spectatorEntity = state.EntityManager.Instantiate(gameResources.SpectatorPrefab);

                state.EntityManager.SetComponentData(spectatorEntity,
                    LocalTransform.FromPositionRotation(spawnPoint.Position, spawnPoint.Rotation));
            }
        }

        private void HandleCharacterSetup(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Initialize local-owned characters
            foreach (var (character, characterInitialized, entity) in SystemAPI
                         .Query<Character, EnabledRefRW<CharacterInitialized>>()
                         .WithAll<GhostOwnerIsLocal, OwningPlayer, GhostOwner>()
                         .WithDisabled<CharacterInitialized>()
                         .WithEntityAccess())
            {
                // Make camera follow character's view
                //ecb.AddComponent(entity, new MainCamera
                //{
                //    BaseFov = 60/*character.BaseFov*/,
                //});

                Debug.LogWarning("Added camera!!!!!!!!!!!!!");
                characterInitialized.ValueRW = true;
                //var childBufferLookup = SystemAPI.GetBufferLookup<Child>();
                //MiscUtilities.SetShadowModeInHierarchy(state.EntityManager, ecb, entity, ref childBufferLookup,
                //    ShadowCastingMode.ShadowsOnly);
            }

            //foreach (var (characterInitialized, entity) in SystemAPI
            //             .Query<EnabledRefRW<CharacterInitialized>>()
            //             .WithAll<Character>()
            //             .WithDisabled<CharacterInitialized>()
            //             .WithEntityAccess())
            //{
            //    //physicsCollider.ValueRW.MakeUnique(entity, ecb);

            //    // Mark initialized
            //    characterInitialized.ValueRW = true;
            //}
        }
    }
}
