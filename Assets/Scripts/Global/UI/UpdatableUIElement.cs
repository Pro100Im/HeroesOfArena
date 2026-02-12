namespace Global.UI
{
    public abstract class UpdatableUIElement<TData> : UIElement<TData>, IUpdatable<TData>
    {
        public abstract void UpdateData(TData data);
    }
}