using UnityEngine;

namespace Global.UI
{
    public abstract class UIElement<TData> : MonoBehaviour, IShowUIElement<TData>, IHideUIElement
    {
        public abstract void Show(TData data);

        public abstract void Hide();
    }
}