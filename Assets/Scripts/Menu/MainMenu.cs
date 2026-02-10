using UnityEngine;
using UnityEngine.UIElements;

namespace Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
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

        private void QuickPlay()
        {
            GameManager.Instance.StartGameAsync(CreationType.QuickJoin);
        }

        private void Test()
        {
            Debug.Log("Test button clicked!");
        }

        private void Exit()
        {
            Application.Quit();
        }

        private void OnDestroy()
        {
            _quickPlayButton.clickable.clicked -= QuickPlay;
            _testButton.clickable.clicked -= Test;
            _exitButton.clickable.clicked -= Exit;
        }
    }
}