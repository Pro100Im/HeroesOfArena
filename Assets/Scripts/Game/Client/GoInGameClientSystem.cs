namespace Game.Client
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Game.GameResources;
    using Global.Network;

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct GoInGameClientSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameResources = SystemAPI.GetSingleton<GameResources>();

            HandleSendJoinRequest(ref state, gameResources);
        }

        private void HandleSendJoinRequest(ref SystemState state, GameResources gameResources)
        {
            if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var clientEntity)
                || SystemAPI.HasComponent<NetworkStreamInGame>(clientEntity))
                return;

            var joinRequestEntity = state.EntityManager.CreateEntity(ComponentType.ReadOnly<ClientJoinRequestRpc>(),
                ComponentType.ReadWrite<SendRpcCommandRequest>());
            var playerName = GameSettings.Instance.PlayerName;
            var clientJoinRequestRpc = new ClientJoinRequestRpc();

            clientJoinRequestRpc.PlayerName.CopyFromTruncated(playerName); // Prevents exceptions on long strings.

            state.EntityManager.SetComponentData(joinRequestEntity, clientJoinRequestRpc);
            state.EntityManager.AddComponentData(clientEntity, new NetworkStreamInGame());
        }
    }
}
