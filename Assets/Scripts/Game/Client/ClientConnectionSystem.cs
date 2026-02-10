using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

//namespace Unity.Template.CompetitiveActionMultiplayer
//{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ClientConnectionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            CompleteDependency();

            if (ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connected ||
                ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connecting)
            {
                bool hasNetworkStreamConnectionSingleton =
                    SystemAPI.TryGetSingleton(out NetworkStreamConnection connection);
                if (hasNetworkStreamConnectionSingleton)
                {
                    ConnectionSettings.Instance.GameConnectionState =
                        connection.CurrentState == ConnectionState.State.Connected
                            ? GameConnectionState.Connected
                            : GameConnectionState.Connecting;
                }

                if (!hasNetworkStreamConnectionSingleton || connection.CurrentState == ConnectionState.State.Unknown)
                {
                    ConnectionSettings.Instance.GameConnectionState = GameConnectionState.Connecting;

                    if (UnityEngine.Time.frameCount % 120 == 0) // Arbitrary rate limit.
                    {
                        Debug.Log($"[{World.Name}] Attempt to [re]connect to {ConnectionSettings.Instance.ConnectionEndpoint}...");

                        ref var driver = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                        driver.Connect(EntityManager, ConnectionSettings.Instance.ConnectionEndpoint);
                    }
                }
            }
        }
    }
//}
