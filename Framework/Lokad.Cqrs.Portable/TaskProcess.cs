using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cqrs
{
    public sealed class TaskProcess : IEngineProcess
    {
        readonly Func<CancellationToken, Task> _factoryToStartTask;

        public TaskProcess(Func<CancellationToken, Task> factoryToStartTask)
        {
            _factoryToStartTask = factoryToStartTask;
        }

        public void Dispose() {}

        public void Initialize() {}

        public Task Start(CancellationToken token)
        {
            return _factoryToStartTask(token);
        }
    }
}