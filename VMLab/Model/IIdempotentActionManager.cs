using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VMLab.Model
{
    public interface IIdempotentActionManager
    {
        void AddAction(IIdempotentAction action);
        void RemoveAction(string name);
        void ClearAction();
        IIdempotentAction[] GetActions();
        IdempotentActionResult TestAction(string name, Hashtable properties);
        IdempotentActionResult UpdateAction(string name, Hashtable properties);
    }

    public class IdempotentActionManager : IIdempotentActionManager
    {

        private readonly List<IIdempotentAction> _actions = new List<IIdempotentAction>();

        public void AddAction(IIdempotentAction action)
        {
            if(_actions.Any(a => a.Name == action.Name))
                throw new IdempotentActionAlreadyExists($"Can't add Idempotent action with name {action.Name} because it already exists!");

            _actions.Add(action);
        }

        public void RemoveAction(string name)
        {
            _actions.RemoveAll(a => a.Name == name);
        }

        public void ClearAction()
        {
            _actions.Clear();
        }

        public IIdempotentAction[] GetActions()
        {
            return _actions.ToArray();
        }

        public IdempotentActionResult TestAction(string name, Hashtable properties)
        {
            var action = _actions.FirstOrDefault(a => a.Name == name);

            if(action == null)
                throw new IdempotentActionDoestnExist($"Can't find Idempotent Action with name {name}");

            foreach (var k in action.RequiredProperties.Where(k => !properties.ContainsKey(k)))
                throw new IdempotentActionPropertyException($"Missing {k} property in idempotent options!");

            var allprops = new List<string>();
            allprops.AddRange(action.OptionalProperties);
            allprops.AddRange(action.RequiredProperties);

            foreach (var k in from object k in properties.Keys where !allprops.Contains(k) select k)
            {
                throw new IdempotentActionPropertyException($"Property {k} is not a valid property for this action!");
            }


            return action.TestAction(properties);
        }

        public IdempotentActionResult UpdateAction(string name, Hashtable properties)
        {
            var testresult = TestAction(name, properties);

            if (testresult == IdempotentActionResult.Ok)
                return testresult;

            if (testresult == IdempotentActionResult.NotConfigured)
            {
                var action = _actions.First(a => a.Name == name);
                var result = action.UpdateAction(properties);

                if(result == IdempotentActionResult.RebootRequired || result == IdempotentActionResult.Failed)
                    return result;
            }

            return TestAction(name, properties);
        }
    }
}
