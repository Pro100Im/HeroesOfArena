using UnityEngine;
using UnityEngine.UIElements;

namespace Global.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TransitionScreen : UpdatableUIElement<string>
    {
        [SerializeField] private UIDocument _transitionScreenDoc;

        private static TransitionScreen _instance;

        private void OnValidate()
        {
            if (_transitionScreenDoc == null)
                _transitionScreenDoc = GetComponent<UIDocument>();
        }

        //private void Awake()
        //{
        //    if (_instance != null && _instance != this)
        //    {
        //        Destroy(gameObject);

        //        return;
        //    }

        //    _instance = this;

        //    DontDestroyOnLoad(gameObject);
        //}

        private void Start()
        {
            UIManager.Register(UIKey.LoadingScreen, this);
        }

        public override void Hide()
        {
            var root = _transitionScreenDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");

            canvas.AddToClassList("hide");
            canvas.pickingMode = PickingMode.Ignore;
        }

        public override void Show(string data)
        {
            var root = _transitionScreenDoc.rootVisualElement;
            var canvas = root.Q<VisualElement>("Canvas");
            var statusText = root.Q<Label>("StatusText");

            statusText.text = data;
            canvas.RemoveFromClassList("hide");
            canvas.pickingMode = PickingMode.Ignore;
        }

        public override void UpdateData(string data)
        {
            var root = _transitionScreenDoc.rootVisualElement;
            var statusText = root.Q<Label>("StatusText");

            statusText.text = data;
        }

        private void OnDestroy()
        {
            UIManager.Unregister(UIKey.LoadingScreen);
        }
    }
}