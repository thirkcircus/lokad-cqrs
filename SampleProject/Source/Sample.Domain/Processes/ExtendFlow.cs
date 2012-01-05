#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2012 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD License
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

namespace Sample.Processes
{
    public static class ExtendFlow
    {


        public static void ToSecurity(this IFunctionalFlow self, ICommand<SecurityId> cmd)
        {
            self.SendCommandsAsBatch(new[] {cmd});
        }

        public static void ToRegistration(this IFunctionalFlow self, ICommand<RegistrationId> cmd)
        {
            self.SendCommandsAsBatch(new[] {cmd});
        }

        public static void ToUser(this IFunctionalFlow self, ICommand<UserId> cmd)
        {
            self.SendCommandsAsBatch(new[] {cmd});
        }

        public static void ToService(this IFunctionalFlow self, IFunctionalCommand cmd)
        {
            self.SendCommandsAsBatch(new ISampleCommand[] {cmd});
        }
    }
}