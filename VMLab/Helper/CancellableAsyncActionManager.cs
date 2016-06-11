using System;
using System.Threading;
using System.Threading.Tasks;
using VMLab.Model;

namespace VMLab.Helper
{
    public interface ICancellableAsyncActionManager
    {
        void Execute(Action action);
        T Execute<T>(Func<T> action);
    }

    public class CancellableAsyncActionManager : ICancellableAsyncActionManager
    {
        private readonly IEnvironmentDetails _environment;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _completed = false;

        public CancellableAsyncActionManager(IEnvironmentDetails environment)
        {
            _environment = environment;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Execute(Action action)
        {
            RunExecute(action);

            while (!_completed)
            {
                if (!_environment.Cmdlet.Stopping) continue;

                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                _cancellationTokenSource.Cancel();
                throw new Exception("Cmdlet stopped!");
            }
        }

        public T Execute<T>(Func<T> action)
        {
            var result = RunExecute(action);

            while (!_completed)
            {
                if (!_environment.Cmdlet.Stopping) continue;

                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                _cancellationTokenSource.Cancel();
                throw new Exception("Cmdlet stopped!");
            }

            return result.Result;
        }

        private async Task<T> RunExecute<T>(Func<T> action)
        {
            _completed = false;

            var result = await Task.Factory.StartNew(action, _cancellationTokenSource.Token);

            _completed = true;

            return result;
        }

        private async void RunExecute(Action action)
        {
            _completed = false;

            try
            {
                await Task.Factory.StartNew(action, _cancellationTokenSource.Token);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to execute cancellable action", e);
            }
            

            _completed = true;
        }
    }
}
