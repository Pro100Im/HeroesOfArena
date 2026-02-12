using System.Collections.Generic;
using UnityEngine;

namespace Global.UI
{
    public static class UIManager
    {
        private static readonly Dictionary<UIKey, object> elements = new();

        private static UIKey? _currentKey;
        private static object _currentElement;

        public static void Register<TData>(UIKey key, IUIElement<TData> element)
        {
            elements.Add(key, element);
        }

        public static void Unregister<TData>(UIKey key, IUIElement<TData> element)
        {
            if (elements.TryGetValue(key, out var registered) && ReferenceEquals(registered, element))
            {
                elements.Remove(key);

                if (_currentKey == key && ReferenceEquals(_currentElement, element))
                {
                    _currentKey = null;
                    _currentElement = null;
                }
            }
        }

        private static IUIElement<TData> Get<TData>(UIKey key)
        {
            if (_currentKey == key && _currentElement is IUIElement<TData> cached)
                return cached;

            if (elements.TryGetValue(key, out var element))
            {
                _currentKey = key;
                _currentElement = element;

                return (IUIElement<TData>)element;
            }

            return null;
        }

        public static void Show<TData>(UIKey key, TData data)
        {
            Debug.Log($"Showing UI element with key: {key} and data: {data}");
            Get<TData>(key)?.Show(data);
        }

        public static void Update<TData>(UIKey key, TData data)
        {
            var element = Get<TData>(key);

            if (element is IUpdatable<TData> updatable)
                updatable.UpdateData(data);
        }

        public static void Hide(UIKey key) 
        { 
            if (_currentKey == key && _currentElement is IUIElement<object> cachedObj) 
            { 
                cachedObj.Hide(); 
                
                return; 
            }
            
            if (elements.TryGetValue(key, out var element) && element is IUIElement<object> uiElement) 
            { 
                uiElement.Hide(); 
            } 
        }
    }
}
