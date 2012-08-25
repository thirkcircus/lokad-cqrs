#region (c) 2010-2012 Lokad - CQRS- New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using SaaS.Client;
using SaaS.Client.Projections.LoginView;
using SaaS.Wires;

namespace SaaS.Web
{
    public static class Global
    {
        //public static readonly HubClient Client;
        public static readonly Client Client;
        public static readonly FormsAuth Forms;
        public static readonly WebAuth Auth;
        public static readonly string CommitId;
        public static readonly IDocumentStore Docs;
        
        static Global()
        {
            CommitId = ConfigurationManager.AppSettings.Get("appharbor.commit_id");

            var integrationPath = AzureSettingsProvider.GetStringOrThrow(Conventions.StorageConfigName);

            
            var contracts = Contracts.CreateStreamer();
            var strategy = new DocumentStrategy();
            if (integrationPath.StartsWith("file:"))
            {
                var path = integrationPath.Remove(0, 5);
                var config = FileStorage.CreateConfig(path);
                
                Docs = config.CreateDocumentStore(strategy);
                var commands = config.CreateMessageSender(contracts, Conventions.DefaultRouterQueue);
                var events = config.CreateMessageSender(contracts, Conventions.FunctionalEventRecorderQueue);

                var sender = new TypedMessageSender(commands, events);
                Client = new Client(Docs, sender, config.CreateStreaming());
            }
            else if (integrationPath.StartsWith("Default") || integrationPath.Equals("UseDevelopmentStorage=true", StringComparison.InvariantCultureIgnoreCase))
            {
                
                var config = AzureStorage.CreateConfig(integrationPath);
                Docs = config.CreateDocumentStore(strategy);
                var commands = config.CreateMessageSender(contracts, Conventions.DefaultRouterQueue);
                var events = config.CreateMessageSender(contracts, Conventions.FunctionalEventRecorderQueue);

                var sender = new TypedMessageSender(commands, events);
                Client = new Client(Docs, sender, config.CreateStreaming());
            }
            else
            {
                throw new InvalidOperationException("Unsupported environment");
            }

         
         

            Forms = new FormsAuth(Docs.GetReader<UserId, LoginView>());
            Auth = new WebAuth(Client);
        }


    }
}