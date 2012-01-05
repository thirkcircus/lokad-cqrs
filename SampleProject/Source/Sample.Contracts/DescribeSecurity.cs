namespace Sample
{
    static class DescribeSecurity
    {
        static string When(CreateSecurityAggregate c)
        {
            return "Create security group";
        }
        static string When(SecurityAggregateCreated e)
        {
            return "Security group created";
        }

        static string When(AddSecurityPassword e)
        {
            return string.Format("Add login '{0}': {1}/{2}", e.DisplayName, e.Login, e.Password);
        }
        static string When(SecurityPasswordAdded e)
        {
            return string.Format("Added login '{0}' as {1} with encrypted pass and salt", e.DisplayName, e.UserId.Id);
        }
        static string When(AddSecurityKey e)
        {
            return string.Format("Add security key '{0}'",e.DisplayName);
        }
        static string When(SecurityKeyAdded e)
        {
            return string.Format("Added key '{0}' as {1}", e.DisplayName, e.UserId.Id);
        }
        static string When(AddSecurityIdentity c)
        {
            return string.Format("Add identity '{0}': {1}", c.DisplayName, c.Identity);
        }
        static string When(SecurityIdentityAdded e)
        {
            return string.Format("Added identity '{0}' as {1}", e.DisplayName, e.UserId.Id);
        }
        
    }
}