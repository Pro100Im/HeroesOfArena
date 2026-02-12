using Global.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Menu
{
    [RequireComponent(typeof(UIDocument))]
    public class SearchingPopup : MonoBehaviour, IUIElement<string>, IUpdatable<string>
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

        public void Show(string data)
        {
            var root = _popupDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");
            var popupText = root.Q<Label>("PopupText");

            popupText.text = data;

            canvas.RemoveFromClassList("hide");
            canvas.pickingMode = PickingMode.Position;
            Debug.Log($"canvas.ClassListContains(\"hide\") {canvas.ClassListContains("hide")}");

            foreach (var child in canvas.Children()) 
                child.pickingMode = PickingMode.Position;
        }

        public void Hide()
        {
            var root = _popupDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");

            canvas.AddToClassList("hide");
            canvas.pickingMode = PickingMode.Ignore;

            foreach (var child in canvas.Children())
                child.pickingMode = PickingMode.Ignore;
        }

        public void UpdateData(string data)
        {
            var root = _popupDoc.rootVisualElement;
            var popupText = root.Q<Label>("PopupText");

            popupText.text = data;
        }

        private void CancelSearching()
        {

        }

        private void OnDestroy()
        {
            _cancelButton.clickable.clicked -= CancelSearching;

            UIManager.Unregister(UIKey.SearchingPopup, this);
        }
    }
}