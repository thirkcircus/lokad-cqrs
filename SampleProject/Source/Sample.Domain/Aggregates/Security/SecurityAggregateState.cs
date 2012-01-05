#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System.Collections.Generic;

namespace Sample.Aggregates.Security
{
    public sealed class SecurityAggregateState : IAggregateState
    {
        public sealed class User
        {
            public string DisplayName { get; set; }
            public bool Removed { get; set; }
            public UserId Id { get; set; }
            public RegistrationId Registration { get; set; }
            public string Token { get; set; }
            public bool Locked { get; set; }
            public readonly string Lookup;

            public User(string lookup)
            {
                Lookup = lookup;
            }
        }

        readonly IDictionary<UserId, User> _globals = new Dictionary<UserId, User>();


        public SecurityId Id { get; private set; }

        public User GetUser(UserId userNum)
        {
            return _globals[userNum];
        }


        public bool TryGetUser(UserId id, out User user)
        {
            return _globals.TryGetValue(id, out user);
        }

        public SecurityAggregateState(IEnumerable<IEvent<IIdentity>> events)
        {
            foreach (var e in events)
            {
                Apply(e);
            }
        }

        public bool ContainsUser(UserId id)
        {
            return _globals.ContainsKey(id);
        }

        public void Apply(IEvent<IIdentity> e)
        {
            RedirectToWhen.InvokeEventOptional(this, e);
        }

        public void When(SecurityItemRemoved e)
        {
            _globals[e.UserId].Removed = true;
        }

        public void When(SecurityAggregateCreated e)
        {
            Id = e.Id;
        }

        public void When(SecurityIdentityAdded e)
        {
            var user = new User(e.Identity)
                {
                    Id = e.UserId,
                    DisplayName = e.DisplayName,
                };
            _globals.Add(e.UserId, user);
        }

        public void When(SecurityPasswordAdded e)
        {
            var user = new User(e.Login)
                {
                    Id = e.UserId,
                    DisplayName = e.DisplayName,
                };
            _globals.Add(e.UserId, user);
        }

        public void When(SecurityKeyAdded e)
        {
            var user = new User(e.Key)
                {
                    Id = e.UserId,
                    DisplayName = e.DisplayName,
                };
            _globals.Add(e.UserId, user);
        }
    }
}