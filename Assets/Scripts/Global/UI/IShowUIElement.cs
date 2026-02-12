namespace Global.UI
{
    public interface IShowUIElement<TData>
    {
        public void Show(TData data);
    }
}