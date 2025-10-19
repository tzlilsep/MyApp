namespace TS.Engine.Abstractions
{
    public interface IAuthService
    {
        // Authenticates the user and returns user ID, ID token, and error if any
        Task<(bool Ok, string? UserId, string? IdToken, string? Error)> SignInAsync(string username, string password);

        // Signs the user out (clears local session/state)
        Task SignOutAsync();

        // Returns the current user ID (kept for compatibility, no cloud session)
        Task<string?> GetUserIdAsync();
    }
}
