using TS.Engine.Abstractions;
using TS.AppPages.ShoppingListApp.UiModels;
using TS.AppPages.ShoppingListApp.ViewModels;

namespace TS.AppPages.ShoppingListApp.Pages;

public partial class EditList : ContentPage
{
    public EditList(string userId, ShoppingListItem item, IShoppingListService svc)
    {
        InitializeComponent();
        BindingContext = new EditListViewModel(this.Navigation, userId, item, svc);

        this.SetBinding(TitleProperty, nameof(EditListViewModel.Title));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EditListViewModel vm)
            await vm.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is EditListViewModel vm)
        {
            // שומרים ברקע – לא await כדי לא לחסום ניווט
            _ = vm.SaveAsync();
        }
    }
}
