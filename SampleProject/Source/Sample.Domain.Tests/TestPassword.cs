namespace Sample
{
    public sealed class TestPassword : PasswordGenerator
    {
        public override string CreatePassword(int length)
        {
            return "generated-" + length;
        }

        public override string CreateSalt()
        {
            return "salt";
        }

        public override string HashPassword(string password, string passwordSalt)
        {
            return password + "+" + passwordSalt;
        }

        public const string Token = "generated-32";
    }
}