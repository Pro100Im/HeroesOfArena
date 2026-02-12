using System.Collections.Generic;

namespace Global.UI
{
    public static class UIManager
    {
        private static readonly Dictionary<UIKey, object> elements = new();

        private static UIKey? _currentKey;
        private static object _currentElement;

        public static void Register<TData>(UIKey key, IShowUIElement<TData> element)
        {
            elements.Add(key, element);
        }

        public static void Unregister(UIKey key)
        {
            if (elements.Remove(key))
            {
                if (_currentKey == key)
                {
                    _currentKey = null;
                    _currentElement = null;
                }
            }
        }

        public static void Show<TData>(UIKey key, TData data)
        {
            var element = Get<TData>(key);

            element.Show(data);
        }

        public static void Update<TData>(UIKey key, TData data)
        {
            var element = Get<TData>(key);

            if (element is IUpdatable<TData> updatable)
                updatable.UpdateData(data);
        }

        private static IShowUIElement<TData> Get<TData>(UIKey key)
        {
            if (_currentKey == key && _currentElement is IShowUIElement<TData> cached)
                return cached;

            if (elements.TryGetValue(key, out var element))
            {
                _currentKey = key;
                _currentElement = element;

                return (IShowUIElement<TData>)element;
            }

            return null;
        }

        public static void Hide(UIKey key)
        {
            if (_currentKey == key && _currentElement is IHideUIElement current)
            {
                current.Hide();

                _currentKey = null;
                _currentElement = null; return;
            }

            if (elements.TryGetValue(key, out var element) && element is IHideUIElement hideElement)
            {
                hideElement.Hide();
            }
        }
    }
}
