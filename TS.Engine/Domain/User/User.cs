namespace TS.Engine.Domain.User
{
    /// <summary>
    /// Represents an application user with a unique ID and display name.
    /// Immutable after creation.
    /// </summary>
    public sealed class User
    {
        // Unique user identifier (e.g., from AWS Cognito)
        public string UserId { get; }

        // Display name for the user
        public string UserName { get; }

        // Constructor initializes immutable user properties
        public User(string userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
    }
}
