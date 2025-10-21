using System.Globalization;
using TS.AppPages.ShoppingListApp.ViewModels;
using TS.Engine.Abstractions;

namespace TS.AppPages.ShoppingListApp;

public partial class ShoppingList : ContentPage
{
    private readonly string _userId;

    // Receives a ready-made service (created in HomePage via factory)
    public ShoppingList(string userId, IShoppingListService svc)
    {
        InitializeComponent();
        _userId = userId;

        // Avoid setting BindingContext in designer to prevent design-time errors
        if (svc != null)
            BindingContext = new ShoppingListViewModel(this.Navigation, _userId, svc);
    }

    // Parameterless constructor for designer only
    public ShoppingList() : this(string.Empty, null!) { }

    // No additional loading is needed on appearing (lists + previews are loaded in VM ctor)
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Intentionally left blank for backward compatibility
    }

    // Inverts a boolean value (true -> false, false -> true)
    public sealed class BoolInverseConverter : IValueConverter
    {
        public static BoolInverseConverter Instance { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }
}
