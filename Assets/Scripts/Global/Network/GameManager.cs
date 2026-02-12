using Global.Navigation;
using Global.Network.Connection;
using Global.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Global.Network
{
    public class GameManager : MonoBehaviour
    {
        public enum GlobalGameState
        {
            MainMenu,
            InGame,
            Loading,
        }

        public enum PlayerState
        {
            Playing,
            Dead,
        }

        public const int MaxPlayer = 16;
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "GameScene";
        public const string ResourcesSceneName = "GameResources";

        public static bool CanUseMainMenu => SceneManager.GetActiveScene().name == MainMenuSceneName;

        public static GameManager Instance { get; private set; }

        private GlobalGameState _gameState;
        private bool _mainMenuSceneLoaded;

        private GameConnection _gameConnection;

        private Task _loadingGame;
        private CancellationTokenSource _loadingGameCancel;
        private Task _loadingMainMenu;
        private CancellationTokenSource _loadingMainMenuCancel;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);

                return;
            }

            Instance = this;
        }

        private async void Start()
        {
            _mainMenuSceneLoaded = false;

            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                _loadingMainMenuCancel = new CancellationTokenSource();

                try
                {
                    _loadingMainMenu = StartMenuAsync(_loadingMainMenuCancel.Token);

                    await _loadingMainMenu;
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do when the task is cancelled.
                }
                finally
                {
                    _loadingMainMenuCancel.Dispose();
                    _loadingMainMenuCancel = null;
                }
            }
        }

        private async Task StartMenuAsync(CancellationToken cancellationToken)
        {
            DestroyLocalSimulationWorld();

            var clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            await ScenesLoader.LoadGameplayAsync(null, clientWorld);

            _mainMenuSceneLoaded = true;

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async void StartGameAsync(CreationType creationType)
        {
            if (_gameState != GlobalGameState.MainMenu)
            {
                Debug.Log("[StartGameAsync] Called but in-game, cannot start while in-game!");
                return;
            }

            UIManager.Show(UIKey.SearchingPopup, "Starting...");

            Debug.Log($"[{nameof(StartGameAsync)}] Called with creation type '{creationType}'");

            BeginEnteringGame();

            _loadingGameCancel = new CancellationTokenSource();

            try
            {
                _loadingGame = StartGameAsync(creationType, _loadingGameCancel.Token);

                await _loadingGame;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{nameof(StartGameAsync)}] Loading has been cancelled.");

                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(StartGameAsync)}] Loading has failed, returning to main menu");
                Debug.LogException(e);

                // Disposing the token here because the error has been handled and ReturnToMainMenu should not check it.
                _loadingGameCancel.Dispose();
                _loadingGameCancel = null;

                ReturnToMainMenuAsync();

                return;
            }
            finally
            {
                _loadingGameCancel?.Dispose();
                _loadingGameCancel = null;
            }

            FinishLoadingGame();
        }

        public void CancelStartingGame()
        {
            _loadingGameCancel?.Cancel();
            
            UIManager.Hide(UIKey.SearchingPopup);
        }

        private async Task StartGameAsync(CreationType creationType, CancellationToken cancellationToken)
        {
            if (_loadingMainMenuCancel != null || _mainMenuSceneLoaded)
            {
                if (_loadingMainMenuCancel != null)
                {
                    _loadingMainMenuCancel.Cancel();

                    try
                    {
                        await _loadingMainMenu;
                    }
                    catch (OperationCanceledException)
                    {
                        // We are ignoring the cancelled exception as it is expected.
                    }
                }

                if (_mainMenuSceneLoaded)
                    await DisconnectAndUnloadWorlds();

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Connecting to a Multiplayer Session.
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.InitializeConnection);

            switch (creationType)
            {
                case CreationType.Create:
                    {
                        _gameConnection = await GameConnection.CreateGameAsync();

                        break;
                    }
                case CreationType.Join:
                    {
                        _gameConnection = await GameConnection.JoinGameAsync();

                        break;
                    }
                case CreationType.QuickJoin:
                    {
                        Debug.Log($"[{nameof(StartGameAsync)}] Quick joining a game.");
                        _gameConnection = await GameConnection.JoinOrCreateMatchmakerGameAsync(cancellationToken);

                        break;
                    }
            }

            _gameConnection.Session.RemovedFromSession += OnSessionLeft;

            cancellationToken.ThrowIfCancellationRequested();

            ConnectionSettings.Instance.SessionCode = _gameConnection.Session.Code;

            UIManager.Update(UIKey.SearchingPopup, "Match found!");

            // Creating entity worlds.
            CreateEntityWorlds(_gameConnection.Session, _gameConnection.SessionConnectionType, out var server, out var client);

            // If we have a server, start listening.
            if (server != null)
            {
                using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;

                serverDriver.Listen(_gameConnection.ListenEndpoint);
            }
            if (client != null)
            {
                ConnectionSettings.Instance.ConnectionEndpoint = _gameConnection.ConnectEndpoint;

                await WaitForPlayerConnectionAsync(cancellationToken);

                UIManager.Hide(UIKey.SearchingPopup);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Worlds are created and connected, game is ready to load and start.
            await ScenesLoader.LoadGameplayAsync(server, client);

            cancellationToken.ThrowIfCancellationRequested();

            if (client != null)
            {
                await WaitForGhostReplicationAsync(client, cancellationToken);
                await WaitForAttachedCameraAsync(client, cancellationToken);
            }

        }

        private void OnSessionLeft()
        {
            _gameConnection = null;

            ReturnToMainMenuAsync();
        }

        private void BeginEnteringGame()
        {
            _gameState = GlobalGameState.Loading;
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.StartLoading);
        }

        private static void CreateEntityWorlds(ISession session, NetworkType connectionType,
            out World serverWorld, out World clientWorld)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.CreateWorld);

            DestroyLocalSimulationWorld();

#if UNITY_EDITOR
            if (connectionType == NetworkType.Relay && MultiplayerPlayModePreferences.RequestedNumThinClients > 0)
            {
                Debug.Log($"[{nameof(CreateEntityWorlds)}] A number of Thin Clients was set while the connection mode is set to use Relay. Disabling Thin Clients.");
                MultiplayerPlayModePreferences.RequestedNumThinClients = 0;
            }
#endif

            serverWorld = null;
            clientWorld = null;

            if (session.IsHost)
            {
                serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            }
            if (!session.IsServer())
            {
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            }
        }

        /// <summary>
        /// Destroy all local game simulation worlds if any before creating new server/client worlds.
        /// </summary>
        private static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        private static async Task DestroyGameSessionWorlds()
        {
            // This prevents the "Cannot dispose world while updating it" error,
            // allowing us to call this from anywhere.
            await Awaitable.EndOfFrameAsync();

            // Destroy netcode worlds:
            for (var i = World.All.Count - 1; i >= 0; i--)
            {
                var world = World.All[i];
                if (world.IsServer() || world.IsClient())
                {
                    world.Dispose();
                }
            }
        }

        private static async Task WaitForPlayerConnectionAsync(CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingConnection);
            // The GameManagerSystem is handling the connection/reconnection once the client world is created.
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.Connecting;

            while (ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connecting)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        private static async Task WaitForGhostReplicationAsync(World world, CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication);

            using var ghostCountQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostCount>());
            var waitedForTicks = 0;

            while (true)
            {
                if (ghostCountQuery.TryGetSingleton<GhostCount>(out var ghostCount))
                {
                    var synchronizingPercentage = ghostCount.GhostCountOnServer == 0
                        ? math.saturate(ghostCount.GhostCountInstantiatedOnClient / (float)ghostCount.GhostCountOnServer)
                        : waitedForTicks > 60 ? 1f : 0f; // Apparently the server has no ghosts to send us, so ghost loading is complete.

                    //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication, synchronizingPercentage);

                    if (synchronizingPercentage > 0.99f) // A bit of wiggle room, because in most games, ghosts are constantly created and destroyed.
                        return;
                }

                await Awaitable.NextFrameAsync(cancellationToken);

                waitedForTicks++;
            }
        }

        private static async Task WaitForAttachedCameraAsync(World world, CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingOnPlayer);

            //using var mainEntityCameraQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MainCamera>());

            //while (!mainEntityCameraQuery.HasSingleton<MainCamera>())
            //{
            //    await Awaitable.NextFrameAsync(cancellationToken);
            //}

            //// Waiting an extra frame so that the player position is properly synced with the server.
            await Awaitable.NextFrameAsync(cancellationToken);
        }

        private void FinishLoadingGame()
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadingDone);
            _gameState = GlobalGameState.InGame;
        }

        public async void ReturnToMainMenuAsync()
        {
            Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Called.");

            if (!CanUseMainMenu)
            {
                QuitAsync();

                return;
            }

            if (_loadingGameCancel != null)
            {
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Cancelling loading game.");

                _loadingGameCancel.Cancel();

                try
                {
                    await _loadingGame;
                }
                catch (OperationCanceledException)
                {
                    // Discarding this exception because we're the one asking for it.
                }

                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Loading Cancelled, start returning to main menu.");
            }

            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingGame);
            _gameState = GlobalGameState.Loading;

            await DisconnectAndUnloadWorlds();

            // Restart the main menu scene.
            Start();

            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.BackToMainMenu);
            _gameState = GlobalGameState.MainMenu;
        }

        private async Task DisconnectAndUnloadWorlds()
        {
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.NotConnected;

            var requestedDisconnect = false;

            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                    if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                    {
                        requestedDisconnect = true;
                        world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                    }
                }
            }

            if (requestedDisconnect)
                await Awaitable.NextFrameAsync();

            await LeaveSessionAsync();
            await DestroyGameSessionWorlds();
            await ScenesLoader.UnloadGameplayScenesAsync();
        }

        private async Task LeaveSessionAsync()
        {
            if (_gameConnection != null)
            {
                _gameConnection.Session.RemovedFromSession -= OnSessionLeft;

                if (_gameConnection.Session.IsHost || _gameConnection.Session.IsServer())
                    ConnectionSettings.Instance.SessionCode = null;

                if (_gameConnection.Session.IsHost)
                    await _gameConnection.Session.AsHost().DeleteAsync();
                else
                    await _gameConnection.Session.LeaveAsync();

                _gameConnection = null;
            }
        }

        public async void QuitAsync()
        {
            await LeaveSessionAsync();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("[GameFlowManager] Application.Quit called!");
            Application.Quit();
#endif
        }
    }
}