#region Copyright (c) 2006-2011 LOKAD SAS. All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed

#endregion

// ReSharper disable InconsistentNaming

using Sample.Aggregates.Login;

namespace Sample.Aggregates.User
{
    public sealed class UserSpec : AggregateSpecification<UserId>
    {
        public UserSpec()
        {
            Factory = (events, observer) =>
                {
                    var state = new UserAggregateState(events);
                    return new UserAggregate(state, observer);
                };
        }
    }
    public sealed class UserFail : AggregateFailSpecification<UserId, DomainError>
    {
        public UserFail()
        {
            Factory = (events, observer) =>
            {
                var state = new UserAggregateState(events);
                return new UserAggregate(state, observer);
            };
        }
    }
}