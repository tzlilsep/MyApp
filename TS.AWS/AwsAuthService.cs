using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using TS.Engine.Abstractions;

namespace TS.AWS
{
    // Implements IAuthService using Amazon Cognito (USER_PASSWORD_AUTH).
    // Handles login and returns UserId + IdToken only.
    public sealed class AwsAuthService : IAuthService
    {
        private readonly IAmazonCognitoIdentityProvider _cognito;
        private readonly string _clientId;

        public AwsAuthService()
        {
            _clientId = AwsAuthConfig.ClientId;

            // Anonymous credentials — no access keys needed for basic login
            _cognito = new AmazonCognitoIdentityProviderClient(
                new AnonymousAWSCredentials(),
                RegionEndpoint.GetBySystemName(AwsAuthConfig.Region));
        }

        public async Task<(bool Ok, string? UserId, string? IdToken, string? Error)> SignInAsync(string username, string password)
        {
            try
            {
                var req = new InitiateAuthRequest
                {
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    AuthParameters = new()
                    {
                        ["USERNAME"] = username,
                        ["PASSWORD"] = password
                    }
                };

                var resp = await _cognito.InitiateAuthAsync(req);

                // Extract ID token and user ID (sub claim)
                var idToken = resp.AuthenticationResult?.IdToken;
                if (string.IsNullOrWhiteSpace(idToken))
                    return (false, null, null, "Missing id_token");

                var userId = JwtClaim(idToken, "sub");
                if (string.IsNullOrWhiteSpace(userId))
                    return (false, null, null, "UserId not found in token");

                return (true, userId, idToken, null);
            }
            catch (NotAuthorizedException)
            {
                return (false, null, null, "Invalid username or password.");
            }
            catch (UserNotConfirmedException)
            {
                return (false, null, null, "User not confirmed.");
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        public Task SignOutAsync() => Task.CompletedTask;

        public Task<string?> GetUserIdAsync() => Task.FromResult<string?>(null);

        // Helper: extract claim value from JWT
        private static string JwtClaim(string jwt, string claim)
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return "";

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty(claim, out var v) ? v.GetString() ?? "" : "";
        }
    }
}
