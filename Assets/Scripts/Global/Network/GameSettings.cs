using System;
using UnityEngine;

namespace Global.Network
{
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
    }
}