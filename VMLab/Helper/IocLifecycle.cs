using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMLab.Helper
{
    public class IocLifecycle
    {
        private readonly IocRule _rule;

        public IocLifecycle(IocRule rule)
        {
            _rule = rule;
        }

        public void Singleton()
        {
            _rule.IsSingleton = true;
        }
    }
}
