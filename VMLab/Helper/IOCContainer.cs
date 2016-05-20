using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VMLab.Model;

namespace VMLab.Helper
{
    public class IocContainer
    {
        private readonly List<IocRule> _rules = new List<IocRule>();

        public IocLifecycle Register<TF, T>(string setname = default(string))
        {
            return Register(typeof(TF), typeof(T), setname);
        }

        public IocLifecycle Register(Type from, Type to, string setname = default(string))
        {
            var rule = new IocRule(from, to, setname);
            _rules.Add(rule);

            return new IocLifecycle(rule);
        }

        public T GetObject<T>(string setname = default(string))
        {
            return (T) GetObject(typeof(T), setname);
        }

        public object GetObject(Type type, string setname = default(string))
        {
            var rules = _rules.Where(r => r.From == type && (r.SetName == default(string) || r.SetName == setname)).ToArray();

            if(rules.Length > 1)
                throw new IocException($"Can't call GetObject for {nameof(type)} because there are multiple types registered for it.");

            var rule = rules.FirstOrDefault();
           
            if (rule == null)
                throw new IocException($"Can't find type {nameof(type)}");

            if (rule.Instance != null)
                return rule.Instance;

            var obj = BuildType(rule.To, setname);

            if (rule.IsSingleton)
                rule.Instance = obj;

            return obj;
        }

        private object BuildType(Type type, string setname)
        {
            var constructorInfo = type.GetConstructors().FirstOrDefault();

            return constructorInfo == null ? 
                Activator.CreateInstance(type) : 
                Activator.CreateInstance(type, constructorInfo.GetParameters().Select(p => GetObject(p.ParameterType, setname)).ToArray());
        }

        public IEnumerable GetObjects(Type type, string setname = default(string))
        {
            return _rules.Where(r => r.From == type && (r.SetName == default(string) || r.SetName == setname))
                .Select(r => BuildType(r.To, setname)).ToList();
        }

        public IEnumerable<T> GetObjects<T>(string setname = default(string))
        {
            return GetObjects(typeof(T), setname).Cast<T>();
        }

        public void RegisterInstance<T>(object regularObject, string setname = default(string))
        {
            _rules.RemoveAll(r => r.From == typeof(T));
            _rules.Add(new IocRule(typeof(T), regularObject.GetType(), setname, regularObject));
        }
    }
}
