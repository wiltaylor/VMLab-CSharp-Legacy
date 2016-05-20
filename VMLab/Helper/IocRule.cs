using System;

namespace VMLab.Helper
{
    public class IocRule
    {
        public Type From { get; }
        public Type To { get; }
        public string SetName { get; }
        public object Instance { get; set; }

        public bool IsSingleton { get; set; }

        public IocRule(Type from, Type to, string setname, object instance = default(object), bool issingleton = false)
        {
            From = from;
            To = to;
            SetName = setname;
            Instance = instance;
            IsSingleton = issingleton;
        }
    }
}
