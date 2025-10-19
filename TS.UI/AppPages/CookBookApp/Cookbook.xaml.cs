namespace TS.AppPages;

public partial class Cookbook : ContentPage
{
    private readonly string _userId;
    private readonly string _idToken;

    // Constructor that receives the user ID and authentication token
    public Cookbook(string userId, string idToken)
    {
        InitializeComponent();
        _userId = userId;
        _idToken = idToken;
    }

    // Empty constructor for XAML designer compatibility (not used at runtime)
    public Cookbook() : this(string.Empty, string.Empty) { }
}
