using System;
using System.Collections.Generic;

namespace Sample.Engine
{
    public static class DemoMessages
    {
        public static IEnumerable<ISampleMessage> Create()
        {
            var security = new SecurityId(0);
            yield return (new CreateSecurityAggregate(security));
            yield return (new AddSecurityPassword(security, "Rinat Abdullin", "contact@lokad.com", "password"));
            yield return (new AddSecurityIdentity(security, "Rinat's Open ID", "http://abdullin.myopenid.org"));
            yield return (new AddSecurityKey(security, "some key"));
        } 
    }
}