using TS.Engine.Abstractions;

namespace TS.AppPages;

public partial class HomePage : ContentPage
{
    private readonly string _userId;
    private readonly string _idToken;
    private readonly IShoppingListServiceFactory _listFactory;

    public HomePage(string userId, string idToken, IShoppingListServiceFactory listFactory)
    {
        InitializeComponent();
        _userId = userId;
        _idToken = idToken;
        _listFactory = listFactory;
    }

    // Default constructor (used when no parameters are provided)
    public HomePage() : this(string.Empty, string.Empty, null!) { }

    // Handle click on the first mini app (Cookbook)
    private async void OnMiniApp1Clicked(object sender, EventArgs e)
    {
        // Navigates to the Cookbook page (recipes app)
        await Navigation.PushAsync(new Cookbook(_userId, _idToken));
    }

    // Handle click on the second mini app (Shopping List)
    private async void OnMiniApp2Clicked(object sender, EventArgs e)
    {
        // Creates a shopping list service instance based on the user's idToken
        var listService = _listFactory.Create(_idToken);

        // Navigates to the Shopping List page, passing user info and service
        await Navigation.PushAsync(new TS.AppPages.ShoppingListApp.ShoppingList(_userId, listService));
    }
}
