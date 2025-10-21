using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TS.Engine.Abstractions;
using TS.Engine.Contracts;
using TS.AppPages.ShoppingListApp.UiModels;

namespace TS.AppPages.ShoppingListApp.ViewModels;

public class EditListViewModel : INotifyPropertyChanged
{
    private readonly IShoppingListService _svc;
    private readonly string _userId;

    public ShoppingListItem ListItem { get; }   // The UI model bound to the edit page
    public string Title => string.IsNullOrWhiteSpace(ListItem.Name) ? "List details" : ListItem.Name;

    private string _newItemText = string.Empty;
    public string NewItemText
    {
        get => _newItemText;
        set
        {
            if (_newItemText != value)
            {
                _newItemText = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddItemCommand { get; }
    public ICommand DeleteItemCommand { get; }

    public EditListViewModel(INavigation nav, string userId, ShoppingListItem item, IShoppingListService svc)
    {
        // 'nav' is kept in the constructor signature for compatibility but is no longer used
        _svc = svc;
        _userId = userId;
        ListItem = item;

        // Update page title when the list name changes
        ListItem.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShoppingListItem.Name))
                OnPropertyChanged(nameof(Title));
        };

        AddItemCommand = new Command(AddItem);
        DeleteItemCommand = new Command<ChecklistItem>(DeleteItem);
    }

    public async Task OnAppearingAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var dto = await _svc.LoadAsync(_userId, ListItem.ListId);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                ListItem.Name = dto.Name;

            ListItem.Items.Clear();
            foreach (var it in dto.Items)
                ListItem.Items.Add(new ChecklistItem { Text = it.Text, IsChecked = it.IsChecked });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditList.Load error: {ex.Message}");
            // Consider using IAlertService for better UX in the future
        }
    }

    private void AddItem()
    {
        var text = NewItemText?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            ListItem.AddItem(text);
            NewItemText = string.Empty;
        }
    }

    private void DeleteItem(ChecklistItem? item)
    {
        if (item != null)
            ListItem.Items.Remove(item);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task SaveAsync()
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
        }
    }
}
