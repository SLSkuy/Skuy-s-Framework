using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Framework
{
    public class AutoEventMonoBehaviour : MonoBehaviour
    {
        private readonly List<Action> _unsubscribeActions = new System.Collections.Generic.List<Action>();

        protected virtual void Start()
        {
            SubscribeAllEvents();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeAllEvents();
        }

        private void SubscribeAllEvents()
        {
            Type type = GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(AutoEventAttribute), true);
                foreach (AutoEventAttribute attr in attributes)
                {
                    SubscribeEvent(method, attr);
                }
            }
        }

        private void UnsubscribeAllEvents()
        {
            foreach (Action unsubscribe in _unsubscribeActions)
            {
                unsubscribe?.Invoke();
            }
            _unsubscribeActions.Clear();
        }

        private void SubscribeEvent(MethodInfo method, AutoEventAttribute attr)
        {
            Type type = GetType();
            FieldInfo providerField = type.GetField(attr.ProviderFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (providerField == null)
            {
                Debug.LogWarning($"找不到字段: {attr.ProviderFieldName}");
                return;
            }

            object provider = providerField.GetValue(this);
            if (provider == null)
            {
                Debug.LogWarning($"字段 {attr.ProviderFieldName} 的值为null");
                return;
            }

            Type providerType = provider.GetType();
            EventInfo eventInfo = providerType.GetEvent(attr.EventName);
            
            if (eventInfo == null)
            {
                Debug.LogWarning($"找不到事件: {attr.EventName}");
                return;
            }

            Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, method);
            eventInfo.AddEventHandler(provider, handler);
            
            _unsubscribeActions.Add(() =>
            {
                eventInfo.RemoveEventHandler(provider, handler);
            });
        }
    }
}
