#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

// ReSharper disable UnusedMember.Global
namespace Lokad.Cqrs.AtomicStorage
{
    /// <summary>
    /// Allows to configure default implementation of <see cref="IAtomicStorageStrategy"/>
    /// </summary>
    public sealed class DefaultAtomicStorageStrategyBuilder : HideObjectMembersFromIntelliSense
    {
        readonly List<Assembly> _extraAssemblies = new List<Assembly>();

        string _folderForSingleton = "atomic-singleton";
        Func<Type, string> _nameForSingleton = type => CleanName(type.Name) + ".pb";
        Func<Type, string> _folderForEntity = type => CleanName("atomic-" +type.Name);
        Func<Type, object, string> _nameForEntity =
            (type, key) => (CleanName(type.Name) + "-" + Convert.ToString(key, CultureInfo.InvariantCulture).ToLowerInvariant()) + ".pb";

        IAtomicStorageSerializer _serializer = new AtomicStorageSerializerWithDataContracts();



        /// <summary>
        /// Provides custom folder for storing singletons.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        public void FolderForSingleton(string folderName)
        {
            _folderForSingleton = folderName;
        }

        /// <summary>
        /// Helper to clean the name, making it suitable for azure storage
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public static string CleanName(string typeName)
        {
            var sb = new StringBuilder();

            bool lastWasUpper = false;
            bool lastWasSymbol = true;

            foreach (var c in typeName)
            {
                var splitRequired = char.IsUpper(c) || !char.IsLetterOrDigit(c);
                if (splitRequired && !lastWasUpper && !lastWasSymbol)
                {
                    sb.Append('-');
                }
                lastWasUpper = char.IsUpper(c);
                lastWasSymbol = !char.IsLetterOrDigit(c);

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Provides custom naming convention for the singleton files
        /// </summary>
        /// <param name="namingConvention">The naming convention.</param>
        public void NameForSingleton(Func<Type,string> namingConvention)
        {
            _nameForSingleton = namingConvention;
        }
        /// <summary>
        /// Provides custom naming convention for entity folders.
        /// </summary>
        /// <param name="namingConvention">The naming convention.</param>
        public void FolderForEntity(Func<Type,string> namingConvention)
        {
            _folderForEntity = namingConvention;
        }

        public void CustomSerializer(IAtomicStorageSerializer serializer)
        {
            _serializer = serializer;
        }

        public void CustomSerializer(Action<object, Type, Stream> serializer, Func<Type, Stream, object> deserializer)
        {
            _serializer = new AtomicStorageSerializerWithDelegates(serializer, deserializer);
        }

        /// <summary>
        /// Provides custom naming convention for entity files.
        /// </summary>
        /// <param name="namingConvention">The naming convention.</param>
        public void NameForEntity(Func<Type,object,string> namingConvention)
        {
            _nameForEntity = namingConvention;
        }






        /// <summary>
        /// Builds new instance of immutable <see cref="IAtomicStorageStrategy"/>
        /// </summary>
        /// <returns></returns>
        public IAtomicStorageStrategy Build()
        {
            return new DefaultAtomicStorageStrategy(
                _folderForSingleton, 
                _nameForSingleton, 
                _folderForEntity, 
                _nameForEntity, _serializer);
        }

    }
}