#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System.Runtime.Serialization;

namespace Sample
{
    /// <summary>
    /// Strongly-typed identity class. Essentially just an ID with a 
    /// distinct type. It introduces strong-typing and speeds up development
    /// on larger projects. Idea by Jeremie, implementation by Rinat
    /// </summary>
    public interface IIdentity
    {
        /// <summary>
        /// Gets the id, converted to a string. Only alphanumerics and '-' are allowed.
        /// </summary>
        /// <returns></returns>
        string GetId();

        /// <summary>
        /// Unique tag (should be unique within the assembly) to distinguish
        /// between different identities, while deserializing.
        /// </summary>
        int Tag { get; }
    }

    [DataContract(Namespace = "Sample")]
    public sealed class NullId : IIdentity
    {
        public const int TagValue = 0;
        public static readonly IIdentity Instance = new NullId();

        public string GetId()
        {
            return "";
        }

        public int Tag
        {
            get { return 0; }
        }
    }

    /// <summary>
    /// Base implementation of <see cref="IIdentity"/>, which implements
    /// equality and ToString once and for all.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public abstract class AbstractIdentity<TKey> : IIdentity
    {
        public abstract TKey Id { get; protected set; }

        public string GetId()
        {
            return Id.ToString();
        }

        [DataMember(Order = 2)]
        public abstract int Tag { get; protected set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var identity = obj as AbstractIdentity<TKey>;

            if (identity != null)
            {
                return identity.Id.Equals(Id) && identity.Tag == Tag;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", GetType().Name.Replace("Id", ""), Id);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id.GetHashCode() * 397) ^ (Tag.GetHashCode());
            }
        }
    }

    public interface IIdentityGenerator
    {
        long GetId();
    }
}