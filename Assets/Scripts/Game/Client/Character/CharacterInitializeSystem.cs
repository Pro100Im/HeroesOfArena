namespace Game.Client.Character
{
    using Game.Character;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.NetCode;
    using UnityEngine;

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateAfter(typeof(GoInGameClientSystem))]
    [BurstCompile]
    public partial struct CharacterInitializeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Character>();
        }

        public void OnUpdate(ref SystemState state)
        {
            HandleCharacterSetup(ref state);
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
                Debug.LogWarning("Added camera!!!!!!!!!!!!!");

                characterInitialized.ValueRW = true;
            }
        }
    }
}