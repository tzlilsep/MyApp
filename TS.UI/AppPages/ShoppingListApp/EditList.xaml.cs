using TS.Engine.Abstractions;
using TS.Engine.Contracts;

namespace TS.AppPages.ShoppingListApp;

public partial class EditList : ContentPage
{
    private readonly string _userId;
    private readonly IShoppingListService _svc;

    // Convenience accessor for the bound shopping list item (view model)
    private ShoppingListItem ListItem => (ShoppingListItem)BindingContext;

    // Receives current user, the list item to edit (as BindingContext), and the service to persist/load data
    public EditList(string userId, ShoppingListItem item, IShoppingListService svc)
    {
        InitializeComponent();
        _userId = userId;
        _svc = svc;

        BindingContext = item;

        // Page title mirrors the list name (fallback to Hebrew default when empty)
        Title = string.IsNullOrWhiteSpace(item.Name) ? "פרטי רשימה" : item.Name;

        // Keep the title in sync when the list name changes
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShoppingListItem.Name))
                Title = string.IsNullOrWhiteSpace(item.Name) ? "פרטי רשימה" : item.Name;
        };
    }

    // Load latest data for this list whenever the page appears
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var dto = await _svc.LoadAsync(_userId, ListItem.ListId);

            // Update list name if available
            if (!string.IsNullOrWhiteSpace(dto.Name))
                ListItem.Name = dto.Name;

            // Replace current items with the loaded items
            ListItem.Items.Clear();
            foreach (var it in dto.Items)
                ListItem.Items.Add(new ChecklistItem { Text = it.Text, IsChecked = it.IsChecked });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditList.Load error: {ex.Message}");
            await DisplayAlert("שגיאה", "טעינת הרשימה נכשלה", "סגור");
        }
    }

    // Add a new checklist item from the entry field
    private void OnAddItemClicked(object sender, EventArgs e)
    {
        var text = NewItemEntry.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            ListItem.AddItem(text);
            NewItemEntry.Text = string.Empty; // Clear input after adding
        }
    }

    // Handle swipe-to-delete for a checklist item
    private void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipe && swipe.CommandParameter is ChecklistItem item)
            ListItem.Items.Remove(item);
    }

    // Save and navigate back to the previous page
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await SaveAsync();
        await Navigation.PopAsync();
    }

    // Persist the current state of the list via the service
    private async Task SaveAsync()
    {
        try
        {
            var dto = new ShoppingListDto(
                UserId: _userId,
                ListId: ListItem.ListId,
                Name: ListItem.Name,
                Items: ListItem.Items.Select(ci => new ItemDto(ci.Text, ci.IsChecked)).ToList()
            );

            await _svc.SaveAsync(dto);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditList.Save error: {ex.Message}");
            await DisplayAlert("שגיאה", "שמירת הרשימה נכשלה", "סגור");
        }
    }
}
