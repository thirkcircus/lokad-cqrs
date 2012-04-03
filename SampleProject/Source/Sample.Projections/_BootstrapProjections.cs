#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System.Collections.Generic;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Sample.AccountLogins;
using Sample.Login;
using Sample.LoginsIndex;
using Sample.Security;

namespace Sample.Projections
{
    public static class BootstrapProjections
    {
        public static IEnumerable<object> BuildProjectionsWithWhenConvention(IDocumentStore factory)
        {
            yield return new AccountLoginsProjection(factory.GetWriter<SecurityId, AccountLoginsView>());
            yield return new LoginViewProjection(factory.GetWriter<UserId, LoginView>());
            yield return new LoginsIndexProjection(factory.GetWriter<unit, LoginsIndexView>());
            yield return new SecurityProjection(factory.GetWriter<SecurityId, SecurityView>());
        }
    }
}