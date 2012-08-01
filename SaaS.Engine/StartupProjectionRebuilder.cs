using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.TapeStorage;
using Mono.Cecil;
using SaaS.Client;
using SaaS.Wires;

namespace SaaS.Engine
{
    public static class StartupProjectionRebuilder
    {
        public static void Rebuild(CancellationToken token, IDocumentStore targetContainer, IAppendOnlyStore stream, Func<IDocumentStore, IEnumerable<object>> projectors)
        {
            var strategy = targetContainer.Strategy;
            var memory = new MemoryStorageConfig();

            var memoryContainer = memory.CreateNuclear(strategy).Container;
            var tracked = new ProjectionInspectingContainer(memoryContainer);

            var projections = new List<object>();
            projections.AddRange(projectors(tracked));

            if (tracked.Buckets.Count != projections.Count())
                throw new InvalidOperationException("Count mismatch");

            var storage = new NuclearStorage(targetContainer);
            var persistedHashes = new Dictionary<string, string>();
            var name = "domain";
            storage.GetEntity<ProjectionHash>(name).IfValue(v => persistedHashes = v.BucketHashes);

            var activeMemoryProjections = projections.Select((projection, i) =>
            {
                var bucketName = tracked.Buckets[i];
                var viewType = tracked.Views[i];

                var projectionHash = GetClassHash(projection.GetType()) + "\r\n" + GetClassHash(viewType);

                bool needsRebuild = !persistedHashes.ContainsKey(bucketName) || persistedHashes[bucketName] != projectionHash;
                return new
                {
                    bucketName,
                    projection,
                    hash = projectionHash,
                    needsRebuild
                };
            }).ToArray();

            foreach (var memoryProjection in activeMemoryProjections)
            {
                if (memoryProjection.needsRebuild)
                {
                    SystemObserver.Notify("[warn] {0} needs rebuild", memoryProjection.bucketName);
                }
                else
                {
                    SystemObserver.Notify("[good] {0} is up-to-date", memoryProjection.bucketName);
                }
            }


            var needRebuild = activeMemoryProjections.Where(x => x.needsRebuild).ToArray();

            if (needRebuild.Length == 0)
            {
                return;
            }


            var watch = Stopwatch.StartNew();

            var wire = new RedirectToDynamicEvent();
            needRebuild.ForEach(x => wire.WireToWhen(x.projection));


            var handlersWatch = Stopwatch.StartNew();
            var records = stream.ReadRecords(0, Int32.MaxValue).Where(r => r.Name != "audit");

            ObserveWhileCan(records, wire, token);

            if (token.IsCancellationRequested)
            {
                SystemObserver.Notify("[warn] Aborting projections before anything was changed");
                return;
            }

            var timeTotal = watch.Elapsed.TotalSeconds;
            var handlerTicks = handlersWatch.ElapsedTicks;
            var timeInHandlers = Math.Round(TimeSpan.FromTicks(handlerTicks).TotalSeconds, 1);
            SystemObserver.Notify("Total Elapsed: {0}sec ({1}sec in handlers)", Math.Round(timeTotal, 0), timeInHandlers);


            // update projections that need rebuild
            foreach (var b in needRebuild)
            {
                // server might shut down the process soon anyway, but we'll be
                // in partially consistent mode (not all projections updated)
                // so at least we blow up between projection buckets
                token.ThrowIfCancellationRequested();

                var bucketName = b.bucketName;
                var bucketHash = b.hash;

                // wipe contents
                targetContainer.Reset(bucketName);
                // write new versions
                var contents = memoryContainer.EnumerateContents(bucketName);
                targetContainer.WriteContents(bucketName, contents);

                // update hash
                storage.UpdateEntityEnforcingNew<ProjectionHash>(name, x =>
                {
                    x.BucketHashes[bucketName] = bucketHash;
                });

                SystemObserver.Notify("[good] Updated View bucket {0}.{1}", name, bucketName);
            }

            // Clean up obsolete views
            var allBuckets = new HashSet<string>(activeMemoryProjections.Select(p => p.bucketName));
            var obsoleteBuckets = persistedHashes.Where(s => !allBuckets.Contains(s.Key)).ToArray();
            foreach (var hash in obsoleteBuckets)
            {
                // quit at this stage without any bad side effects
                if (token.IsCancellationRequested)
                    return;

                var bucketName = hash.Key;
                SystemObserver.Notify("[warn] {0} is obsolete", bucketName);
                targetContainer.Reset(bucketName);

                storage.UpdateEntityEnforcingNew<ProjectionHash>(name, x => x.BucketHashes.Remove(bucketName));

                SystemObserver.Notify("[good] Cleaned up obsolete view bucket {0}.{1}", name, bucketName);
            }
        }



        [DataContract]
        public sealed class ProjectionHash
        {
            [DataMember(Order = 1)]
            public Dictionary<string, string> BucketHashes { get; set; }

            public ProjectionHash()
            {
                BucketHashes = new Dictionary<string, string>();
            }
        }


        sealed class ProjectionInspectingContainer : IDocumentStore
        {
            readonly IDocumentStore _real;

            public ProjectionInspectingContainer(IDocumentStore real)
            {
                _real = real;
            }

            public readonly List<string> Buckets = new List<string>();
            public readonly List<Type> Views = new List<Type>();

            public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
            {
                Buckets.Add(_real.Strategy.GetEntityBucket<TEntity>());
                Views.Add(typeof(TEntity));
                return _real.GetWriter<TKey, TEntity>();
            }

            public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
            {
                return _real.GetReader<TKey, TEntity>();
            }

            public IDocumentStrategy Strategy
            {
                get { return _real.Strategy; }
            }

            public IEnumerable<DocumentRecord> EnumerateContents(string bucket)
            {
                return _real.EnumerateContents(bucket);
            }

            public void WriteContents(string bucket, IEnumerable<DocumentRecord> records)
            {
                _real.WriteContents(bucket, records);
            }

            public void Reset(string bucket)
            {
                _real.Reset(bucket);
            }
        }

        static readonly IEnvelopeStreamer Streamer = Contracts.CreateStreamer();

        static string GetClassHash(Type type1)
        {
            var location = type1.Assembly.Location;
            var mod = ModuleDefinition.ReadModule(location);
            var builder = new StringBuilder();
            var type = type1;


            var typeDefinition = mod.GetType(type.FullName);
            builder.AppendLine(typeDefinition.Name);
            ProcessMembers(builder, typeDefinition);

            // we include nested types
            foreach (var nested in typeDefinition.NestedTypes)
            {
                ProcessMembers(builder, nested);
            }

            return builder.ToString();
        }

        static void ProcessMembers(StringBuilder builder, TypeDefinition typeDefinition)
        {
            foreach (var md in typeDefinition.Methods.OrderBy(m => m.ToString()))
            {
                builder.AppendLine("  " + md);

                foreach (var instruction in md.Body.Instructions)
                {
                    // we don't care about offsets
                    instruction.Offset = 0;
                    builder.AppendLine("    " + instruction);
                }
            }
            foreach (var field in typeDefinition.Fields.OrderBy(f => f.ToString()))
            {
                builder.AppendLine("  " + field);
            }
        }


        static void ObserveWhileCan(IEnumerable<DataWithName> records, RedirectToDynamicEvent wire, CancellationToken token)
        {
            var date = DateTime.MinValue;
            var watch = Stopwatch.StartNew();
            foreach (var record in records)
            {
                if (token.IsCancellationRequested)
                    return;

                var env = Streamer.ReadAsEnvelopeData(record.Data);
                if (date.Month != env.CreatedOnUtc.Month)
                {
                    date = env.CreatedOnUtc;
                    SystemObserver.Notify("Observing {0:yyyy-MM-dd} {1}", date,
                        Math.Round(watch.Elapsed.TotalSeconds, 2));
                    watch.Restart();
                }
                foreach (var item in env.Items)
                {
                    if ((item.Content is IEvent))
                    {
                        wire.InvokeEvent(item.Content);
                    }
                }
            }
        }
    }

    [DataContract]
    public sealed class ProjectionHash
    {
        [DataMember(Order = 1)]
        public IDictionary<string, string> Entries { get; set; }

        public ProjectionHash()
        {
            Entries = new Dictionary<string, string>();
        }
    }


}