using System;
using UnityEngine;


    public enum GlobalGameState
    {
        MainMenu,
        InGame,
        Loading,
    }

    public enum MainMenuState
    {
        MainMenuScreen,
        DirectConnectPopUp,
        JoinCodePopUp,
    }

    public enum PlayerState
    {
        Playing,
        Dead,
    }

    public class GameSettings
    {
        public static GameSettings Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new GameSettings();

        private const string _playerNameKey = "PlayerName";

        private GameSettings()
        {
            _playerName = PlayerPrefs.GetString(_playerNameKey, Environment.UserName);
        }
 
        private GlobalGameState _gameState;

        public GlobalGameState GameState
        {
            get => _gameState;
            set
            {
                if (_gameState == value)
                    return;

                _gameState = value;
            }
        }

        private MainMenuState _mainMenuState;

        public MainMenuState MainMenuState
        {
            get => _mainMenuState;
            set
            {
                if (_mainMenuState == value)
                    return;

                _mainMenuState = value;
            }
        }

        private string _playerName;

        public string PlayerName
        {
            get => _playerName;
            set
            {
                if (_playerName == value)
                    return;

                _playerName = value;

                PlayerPrefs.SetString(_playerNameKey, value);
            }
        }

        private bool _mainMenuSceneLoaded;
        public bool MainMenuSceneLoaded
        {
            get => _mainMenuSceneLoaded;
            set
            {
                if (_mainMenuSceneLoaded == value)
                    return;

                _mainMenuSceneLoaded = value;
            }
        }
    }
