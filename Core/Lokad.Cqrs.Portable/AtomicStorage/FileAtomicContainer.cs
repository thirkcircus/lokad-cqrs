using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lokad.Cqrs.AtomicStorage
{
    public sealed class FileAtomicContainer : IAtomicContainer
    {
        readonly string _folderPath;
        readonly IAtomicStorageStrategy _strategy;

        public FileAtomicContainer(string folderPath, IAtomicStorageStrategy strategy)
        {
            _folderPath = folderPath;
            _strategy = strategy;
        }

        public override string ToString()
        {
            return new Uri(Path.GetFullPath(_folderPath)).AbsolutePath;
        }

       
        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();


        public IAtomicWriter<TKey, TEntity> GetEntityWriter<TKey, TEntity>()
        {
            var container = new FileAtomicReaderWriter<TKey, TEntity>(_folderPath, _strategy);
            if (_initialized.Add(Tuple.Create(typeof(TKey),typeof(TEntity))))
            {
                container.InitIfNeeded();
            }
            return container;
        }

        public IAtomicReader<TKey, TEntity> GetEntityReader<TKey, TEntity>()
        {
            return new FileAtomicReaderWriter<TKey, TEntity>(_folderPath, _strategy);
        }

        public IAtomicStorageStrategy Strategy
        {
            get { return _strategy; }
        }


        public IEnumerable<AtomicRecord> EnumerateContents()
        {
            var dir = new DirectoryInfo(_folderPath);
            if (dir.Exists)
            {
                var fullFolder = dir.FullName;
                foreach (var info in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var fullName = info.FullName;
                    var path = fullName.Remove(0, fullFolder.Length + 1).Replace(Path.DirectorySeparatorChar,'/');
                    yield return new AtomicRecord(path, () => File.ReadAllBytes(fullName));
                }
            }
        }

        public void WriteContents(IEnumerable<AtomicRecord> records)
        {
            foreach (var pair in records)
            {
                var combine = Path.Combine(_folderPath, pair.Path);
                var path = Path.GetDirectoryName(combine) ?? "";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                File.WriteAllBytes(combine, pair.Read());
            }
        }

        public void Reset()
        {
            if (Directory.Exists(_folderPath))
                Directory.Delete(_folderPath, true);
            Directory.CreateDirectory(_folderPath);
        }
    }
}