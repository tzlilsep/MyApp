namespace TS.AWS
{
    public static class AwsAuthConfig
    {
        // AWS region
        public const string Region = "eu-central-1";

        // Cognito User Pool configuration
        public const string UserPoolId = "eu-central-1_dT2wx55fl"; // Replace with your own
        public const string ClientId = "1tg89uidfufi7l473am7nso1uf"; // Existing client ID

        // Cognito Identity Pool (for federated identities)
        public const string IdentityPoolId = "eu-central-1:2423db26-9393-4277-9105-6895d263281d"; // Replace with your own

        // Provider string linking the IdToken to the Identity Pool
        public static string LoginProvider =>
            $"cognito-idp.{Region}.amazonaws.com/{UserPoolId}";
    }
}
