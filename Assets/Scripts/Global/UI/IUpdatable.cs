using UnityEngine;

namespace Global.UI
{
    public interface IUpdatable<TData>
    {
        public void UpdateData(TData data);
    }
}