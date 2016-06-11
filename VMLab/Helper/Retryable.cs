using System;
using System.Threading;
using System.Threading.Tasks;

namespace VMLab.Helper
{
    public interface IRetryable
    {
        T Run<T>(int timeout, int attempts, Func<T> action);
    }

    public class Retryable : IRetryable
    {
        private bool _completed;

        public T Run<T>(int timeout, int attempts, Func<T> action)
        {
            var result = RunCode(attempts, timeout, action);

            while (!_completed)
            {
                Thread.Sleep(1000);
            }

            return result.Result;
        }

        private async Task<T> RunCode<T>(int attempts, int timeout, Func<T> action)
        {
            var result = default(T);
            _completed = false;

            while (attempts > 0)
            {
                try
                {
                    var canceltoken = new CancellationTokenSource();
                    canceltoken.CancelAfter(timeout);
                    canceltoken.Token.ThrowIfCancellationRequested();
                    result = await Task.Factory.StartNew(action, canceltoken.Token);
                    break;
                }
                catch
                {
                    attempts--;

                    if (attempts == 0)
                        throw;
                }
            }

            _completed = true;

            return result;
        }
    }
}
