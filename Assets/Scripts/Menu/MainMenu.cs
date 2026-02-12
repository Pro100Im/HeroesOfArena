using Global.Network;
using Global.Network.Connection;
using Global.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : UIElement<string>
    {
        [SerializeField] private UIDocument _mainMenuDoc;

        private Button _quickPlayButton;
        private Button _testButton;
        private Button _exitButton;

        private void OnValidate()
        {
            if(_mainMenuDoc == null)
                _mainMenuDoc = GetComponent<UIDocument>();
        }

        private void Awake()
        {
            var root = _mainMenuDoc.rootVisualElement;

            _quickPlayButton = root.Q<Button>("QuickPlayButton");
            _testButton = root.Q<Button>("TestButton");
            _exitButton = root.Q<Button>("ExitButton");

            _quickPlayButton.clickable.clicked += QuickPlay;
            _testButton.clickable.clicked += Test;
            _exitButton.clickable.clicked += Exit;
        }

        private void Start() 
        { 
            UIManager.Register(UIKey.MainMenu, this); 
        }

        public override void Show(string data)
        {

        }

        public override void Hide()
        {

        }

        private void QuickPlay()
        {
            GameManager.Instance.StartGameAsync(CreationType.QuickJoin);
        }

        private void Test()
        {
            Debug.Log("Test button clicked!");
            UIManager.Show(UIKey.SearchingPopup, "test popup!");
        }

        private void Exit()
        {
            GameManager.Instance.QuitAsync();
        }

        private void OnDestroy()
        {
            _quickPlayButton.clickable.clicked -= QuickPlay;
            _testButton.clickable.clicked -= Test;
            _exitButton.clickable.clicked -= Exit;

            UIManager.Unregister(UIKey.MainMenu);
        }
    }
}