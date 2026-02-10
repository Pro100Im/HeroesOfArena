using Unity.Networking.Transport;
using UnityEngine;

    public enum GameConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Matchmaking,
    }

    public enum ConnectionType
    {
        Relay = 0,
        Direct = 1,
    }

    public enum MatchmakerType
    {
        P2P = 0,
        Dgs = 1,
    }

    public enum CreationType
    {
        Create = 0,
        Join = 1,
        QuickJoin = 2,
    }

    public class ConnectionSettings
    {
        public static ConnectionSettings Instance { get; private set; } = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new ConnectionSettings();

        public const string DefaultServerAddress = "127.0.0.1";
        public const ushort DefaultServerPort = 7979;

        private const string _iPAddressKey = "IPAddress";
        private const string _portKey = "Port";

        public NetworkEndpoint ConnectionEndpoint;

        private ConnectionSettings()
        {
            IPAddress = PlayerPrefs.GetString(_iPAddressKey, DefaultServerAddress);

            if (!NetworkEndpoint.TryParse(IPAddress, 0, out _))
                IPAddress = DefaultServerAddress;

            Port = PlayerPrefs.GetString(_portKey, DefaultServerPort.ToString());

            if (!ushort.TryParse(Port, out _))
                Port = DefaultServerPort.ToString();
        }

        private GameConnectionState _gameConnectionState;

        public GameConnectionState GameConnectionState
        {
            get => _gameConnectionState;
            set
            {
                if (_gameConnectionState == value)
                    return;

                _gameConnectionState = value;
            }
        }


        private bool _isNetworkEndpointFormatValid;

        public bool IsNetworkEndpointValid
        {
            get => _isNetworkEndpointFormatValid;
            set
            {
                if (_isNetworkEndpointFormatValid == value)
                    return;

                _isNetworkEndpointFormatValid = value;
            }
        }

        private string _ipAddress;

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress == value)
                    return;

                _ipAddress = value;

                PlayerPrefs.SetString(_iPAddressKey, value);

                IsNetworkEndpointValid = NetworkEndpoint.TryParse(_ipAddress, 0, out _) && ushort.TryParse(_port, out _);
            }
        }

        private string _port;

        public string Port
        {
            get => _port;
            set
            {
                if (_port == value)
                    return;

                _port = value;

                PlayerPrefs.SetString(_portKey, value);

                IsNetworkEndpointValid = NetworkEndpoint.TryParse(_ipAddress, 0, out _) && ushort.TryParse(_port, out _);
            }
        }

        private bool _isSessionCodeFormatValid;

        public bool IsSessionCodeFormatValid
        {
            get => _isSessionCodeFormatValid;
            private set
            {
                if (_isSessionCodeFormatValid == value)
                    return;

                _isSessionCodeFormatValid = value;
            }
        }

        private string _sessionCode;

        public string SessionCode
        {
            get => _sessionCode;
            set
            {
                if(_sessionCode == value)
                    return;

                _sessionCode = value;
                IsSessionCodeFormatValid = CheckIsSessionCodeFormatValid(_sessionCode);
            }
        }

        private static bool CheckIsSessionCodeFormatValid(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length != 6)
                return false;

            foreach (var c in str)
            {
                if (!char.IsLetter(c) && !char.IsNumber(c))
                    return false;
            }

            return true;
        }
    }
