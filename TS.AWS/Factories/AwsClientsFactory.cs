using Amazon;
using Amazon.DynamoDBv2;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using TS.AWS.Auth;

namespace TS.AWS.Factories
{
    public static class AwsClientsFactory
    {
        // Creates a DynamoDB client using a user's IdToken via Cognito Identity Pool.
        // AWS automatically issues temporary credentials based on the IdToken.
        public static IAmazonDynamoDB CreateDynamoDbFromIdToken(string idToken)
        {
            var region = RegionEndpoint.GetBySystemName(AwsAuthConfig.Region);

            // Get temporary credentials through the Identity Pool
            var creds = new CognitoAWSCredentials(AwsAuthConfig.IdentityPoolId, region);

            // Attach the IdToken from the User Pool as a "Login" provider
            creds.AddLogin(AwsAuthConfig.LoginProvider, idToken);

            // Return DynamoDB client with permissions granted by the associated IAM role
            return new AmazonDynamoDBClient(creds, region);
        }
    }
}
