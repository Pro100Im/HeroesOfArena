using Global.Network;
using Global.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class SearchingPopup : UpdatableUIElement<string>
    {
        [SerializeField] private UIDocument _popupDoc;

        private Button _cancelButton;

        private void OnValidate()
        {
            if (_popupDoc == null)
                _popupDoc = GetComponent<UIDocument>();
        }

        private void Awake()
        {
            var root = _popupDoc.rootVisualElement;

            _cancelButton = root.Q<Button>("CancelButton");
            _cancelButton.clickable.clicked += CancelSearching;
        }

        private void Start()
        {
            UIManager.Register(UIKey.SearchingPopup, this);
        }

        public override void Show(string data)
        {
            var root = _popupDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");
            var button = root.Q<Button>("CancelButton");
            var background = root.Q<VisualElement>("Back");
            var popupText = root.Q<Label>("PopupText");

            popupText.text = data;
            canvas.RemoveFromClassList("hide");
            button.pickingMode = PickingMode.Position;
            background.pickingMode = PickingMode.Position;
        }

        public override void Hide()
        {
            var root = _popupDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");
            var button = root.Q<Button>("CancelButton");
            var background = root.Q<VisualElement>("Back");

            canvas.AddToClassList("hide");
            button.pickingMode = PickingMode.Ignore;
            background.pickingMode = PickingMode.Ignore;
        }

        public override void UpdateData(string data)
        {
            var root = _popupDoc.rootVisualElement;
            var popupText = root.Q<Label>("PopupText");

            popupText.text = data;
        }

        private void CancelSearching()
        {
            GameManager.Instance.CancelStartingGame();
        }

        private void OnDestroy()
        {
            _cancelButton.clickable.clicked -= CancelSearching;

            UIManager.Unregister(UIKey.SearchingPopup);
        }
    }
}