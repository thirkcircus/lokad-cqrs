//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using Lokad.Cqrs.Core.Outbox;
//using Lokad.Cqrs.Feature.StreamingStorage;

//namespace Lokad.Cqrs.Feature.TimerService
//{
//    public sealed class FileTimerService : IEngineProcess
//    {
//        IQueueWriterFactory _factory;
//        DirectoryInfo _info;


//        public void Dispose()
//        {
//        }

//        public void Enqueue(ImmutableEnvelope envelope)
//        {
//            // first save to the persistent store and then updated the in-memory
//        }

//        public void Initialize()
//        {
//            // rebuild the timer list
//        }

//        public Task Start(CancellationToken token)
//        {
//            // keep on checking the in-memory queue
//        }
//    }
//}