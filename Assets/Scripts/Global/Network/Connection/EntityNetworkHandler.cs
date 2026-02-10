using System.Threading.Tasks;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;

namespace Global.Network.Connection
{
    class EntityNetworkHandler : INetworkHandler
    {
        public Task<NetworkEndpoint> ConnectEndpoint => m_ConnectEndpoint.Task;
        public Task<NetworkEndpoint> ListenEndpoint => m_ListenEndpoint.Task;
        public Task<NetworkType> SessionConnectionType => m_SessionConnectionType.Task;

        readonly TaskCompletionSource<NetworkEndpoint> m_ConnectEndpoint = new();
        readonly TaskCompletionSource<NetworkEndpoint> m_ListenEndpoint = new();
        readonly TaskCompletionSource<NetworkType> m_SessionConnectionType = new();

        public Task StartAsync(NetworkConfiguration configuration)
        {
            NetworkStreamReceiveSystem.DriverConstructor = new EntityDriverConstructor(configuration);
            m_ConnectEndpoint.SetResult(GetConnectEndpoint(configuration));
            m_ListenEndpoint.SetResult(GetListenEndpoint(configuration));
            m_SessionConnectionType.SetResult(configuration.Type);
            return Task.CompletedTask;
        }

        static NetworkEndpoint GetConnectEndpoint(NetworkConfiguration configuration)
        {
            switch (configuration.Type, configuration.Role)
            {
                case (NetworkType.Direct, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.DirectNetworkListenAddress.Port);
                case (NetworkType.Direct, NetworkRole.Client):
                    return configuration.DirectNetworkPublishAddress;
                case (NetworkType.Relay, NetworkRole.Host):
                    return NetworkEndpoint.LoopbackIpv4.WithPort(configuration.RelayServerData.Endpoint.Port);
                case (NetworkType.Relay, NetworkRole.Client):
                    return configuration.RelayClientData.Endpoint;
                default:
                    return default;
            }
        }

        static NetworkEndpoint GetListenEndpoint(NetworkConfiguration configuration)
        {
            switch (configuration.Type)
            {
                case NetworkType.Direct:
                    return configuration.DirectNetworkListenAddress;
                case NetworkType.Relay:
                    return NetworkEndpoint.AnyIpv4;
                default:
                    return default;
            }
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
}

