#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

namespace Sample.Processes
{
    /// <summary>
    /// Replicates changes between <see cref="ISecurityAggregate"/> and 
    /// individual instances of <see cref="IUserAggregate"/>
    /// </summary>
    public sealed class ReplicationProcess
    {
        readonly IFunctionalFlow _flow;

        public ReplicationProcess(IFunctionalFlow flow)
        {
            _flow = flow;
        }


        public void When(SecurityPasswordAdded e)
        {
            _flow.ToUser(new CreateUser(e.UserId, e.Id));
        }

        public void When(SecurityIdentityAdded e)
        {
            _flow.ToUser(new CreateUser(e.UserId, e.Id));
        }

        public void When(SecurityKeyAdded e)
        {
            _flow.ToUser(new CreateUser(e.UserId, e.Id));
        }

        public void When(SecurityItemRemoved e)
        {
            _flow.ToUser(new DeleteUser(e.UserId));
        }
    }
}