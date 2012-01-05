#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;

namespace Sample.Aggregates.Security
{
    public sealed class SecurityAggregate : IAggregate<SecurityId>, ISecurityAggregate
    {
        readonly Action<IEvent<SecurityId>> _observer;
        readonly SecurityAggregateState _state;
        readonly PasswordGenerator _generator;
        readonly IIdentityGenerator _identityGenerator;

        public SecurityAggregate(SecurityAggregateState state, Action<IEvent<SecurityId>> observer,
            PasswordGenerator generator, IIdentityGenerator identityGenerator)
        {
            _state = state;
            _observer = observer;
            _generator = generator;
            _identityGenerator = identityGenerator;
        }

        public void Execute(ICommand<SecurityId> c)
        {
            RedirectToWhen.InvokeCommand(this, c);
        }

        void Apply(IEvent<SecurityId> e)
        {
            _state.Apply(e);
            _observer(e);
        }

        public void When(AddSecurityPassword c)
        {
            var user = new UserId(_identityGenerator.GetId());
            var salt = _generator.CreateSalt();
            var token = _generator.CreateToken();
            var hash = _generator.HashPassword(c.Password, salt);
            Apply(new SecurityPasswordAdded(c.Id, user, c.DisplayName, c.Login, hash, salt, token));
        }

        public void When(AddSecurityKey c)
        {
            var key = _generator.CreatePassword(32);
            var user = new UserId(_identityGenerator.GetId());
            var token = _generator.CreateToken();
            Apply(new SecurityKeyAdded(c.Id, user, c.DisplayName, key, token));
        }


        public void When(AddSecurityIdentity c)
        {
            var user = new UserId(_identityGenerator.GetId());
            var token = _generator.CreateToken();
            Apply(new SecurityIdentityAdded(c.Id, user, c.DisplayName, c.Identity, token));
        }

        public void When(CreateSecurityAggregate c)
        {
            Apply(new SecurityAggregateCreated(c.Id));
        }

        public void When(CreateSecurityFromRegistration c)
        {
            Apply(new SecurityAggregateCreated(c.Id));
            var user = new UserId(_identityGenerator.GetId());
            var salt = _generator.CreateSalt();
            var token = _generator.CreateToken();
            var hash = _generator.HashPassword(c.Pwd, salt);

            Apply(new SecurityPasswordAdded(c.Id, user, c.DisplayName, c.Login, hash, salt, token));
            if (!string.IsNullOrEmpty(c.OptionalIdentity))
            {
                var u2 = new UserId(_identityGenerator.GetId());

                Apply(new SecurityIdentityAdded(c.Id, u2, c.DisplayName, c.OptionalIdentity, _generator.CreateToken()));
            }
            Apply(new SecurityRegistrationProcessCompleted(c.Id, c.DisplayName, user, token, c.RegistrationId));
        }

        public void When(RemoveSecurityItem c)
        {
            SecurityAggregateState.User user;
            if (!_state.TryGetUser(c.UserId, out user))
            {
                throw new InvalidOperationException("User not found");
            }
            Apply(new SecurityItemRemoved(_state.Id, user.Id, user.Lookup));
        }


        public void When(UpdateSecurityItemDisplayName c)
        {
            var user = _state.GetUser(c.UserId);

            Apply(new SecurityItemDisplayNameUpdated(c.Id, c.UserId, c.DisplayName));
        }
    }
}