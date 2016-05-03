using System.CodeDom;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace VMLab.Model
{
    public enum IdempotentActionResult
    {
        Ok,
        Failed,
        RebootRequired,
        NotConfigured
    }


    public interface IIdempotentAction
    {
        string Name { get;  }
        ScriptBlock Test { get; }
        ScriptBlock Update { get; }
        string[] RequiredProperties { get; }
        string[] OptionalProperties { get; }

        IdempotentActionResult TestAction(Hashtable properties);
        IdempotentActionResult UpdateAction(Hashtable properties);
    }

    public class IdempotentAction : IIdempotentAction
    {
        public string Name { get; }
        public ScriptBlock Test { get; }
        public ScriptBlock Update { get; }
        public string[] RequiredProperties { get; }
        public string[] OptionalProperties { get; }

        public IdempotentAction(string name, ScriptBlock test, ScriptBlock update, string[] required, string[] optional)
        {
            Name = name;
            Test = test;
            Update = update;
            RequiredProperties = required;
            OptionalProperties = optional;
        }

        public IdempotentActionResult TestAction(Hashtable properties)
        {
            var results = Test.Invoke(properties).FirstOrDefault();

            if(results?.BaseObject == null)
                return IdempotentActionResult.NotConfigured;

            if(results.BaseObject is bool && (bool)results.BaseObject)
                return IdempotentActionResult.Ok;

            var s = results.BaseObject as string;
            if(s != null && s == "reboot")
                return IdempotentActionResult.RebootRequired;

            if (results.BaseObject is bool && !(bool)results.BaseObject)
                return IdempotentActionResult.NotConfigured;

            return IdempotentActionResult.Failed;
            
        }

        public IdempotentActionResult UpdateAction(Hashtable properties)
        {
            var results = Update.Invoke(properties).FirstOrDefault();

            if (results?.BaseObject == null)
                return IdempotentActionResult.NotConfigured;

            if (results.BaseObject is bool && (bool)results.BaseObject)
                return IdempotentActionResult.Ok;

            if (results.BaseObject is bool && !(bool)results.BaseObject)
                return IdempotentActionResult.NotConfigured;

            var s = results.BaseObject as string;
            if (s != null && s == "reboot")
                return IdempotentActionResult.RebootRequired;

            return IdempotentActionResult.Failed;
        }
    }
}
