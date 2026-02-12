namespace Global.UI
{
    public interface IUIElement<TData>
    {
        public void Show(TData data);
        public void Hide();
    }
}