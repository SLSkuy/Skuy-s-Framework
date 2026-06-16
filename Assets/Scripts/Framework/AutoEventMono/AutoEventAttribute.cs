using System;

namespace Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AutoEventAttribute : Attribute
    {
        public string EventName { get; }
        public string ProviderFieldName { get; }

        public AutoEventAttribute(string eventName, string providerFieldName)
        {
            EventName = eventName;
            ProviderFieldName = providerFieldName;
        }
    }
}
