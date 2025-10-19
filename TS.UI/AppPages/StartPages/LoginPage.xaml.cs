using TS.Engine.Abstractions;

namespace TS.AppPages;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly IShoppingListServiceFactory _listFactory;

    // Constructor receives authentication service and shopping list factory
    public LoginPage(IAuthService auth, IShoppingListServiceFactory listFactory)
    {
        InitializeComponent();
        _auth = auth;
        _listFactory = listFactory;
    }

    // Triggered when the user clicks the login button
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Read and clean user input
        var name = NameEntry?.Text?.Trim() ?? "";
        var password = PasswordEntry?.Text?.Trim() ?? "";

        // Validate input fields
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Please enter a username and password.", "OK");
            return;
        }

        // Disable button and show loading state
        if (sender is Button btn) btn.IsEnabled = false;
        IsBusy = true;

        try
        {
            // Attempt to sign in
            var (ok, userId, idToken, err) = await _auth.SignInAsync(name, password);

            // Handle failed authentication
            if (!ok || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                await DisplayAlert("Error", err ?? "Login failed.", "OK");
                return;
            }

            // Navigate to HomePage with authenticated user details
            await Navigation.PushAsync(new HomePage(userId, idToken, _listFactory));

            // Remove LoginPage from navigation stack so user can't go back
            var current = Navigation.NavigationStack.FirstOrDefault();
            if (current is LoginPage)
                Navigation.RemovePage(current);
        }
        catch (Exception ex)
        {
            // Show any unexpected error
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            // Restore UI state
            IsBusy = false;
            if (sender is Button b2) b2.IsEnabled = true;
        }
    }
}
