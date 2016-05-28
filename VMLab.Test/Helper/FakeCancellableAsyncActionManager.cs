using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMLab.Helper;

namespace VMLab.Test.Helper
{
    public class FakeCancellableAsyncActionManager : ICancellableAsyncActionManager
    {
        public void Execute(Action action)
        {
            action.Invoke();
        }

        public T Execute<T>(Func<T> action)
        {
            return action.Invoke();
        }
    }
}
