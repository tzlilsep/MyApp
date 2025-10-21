using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TS.Engine.Abstractions;
using TS.AppPages.ShoppingListApp.UiModels;
using TS.AppPages.ShoppingListApp.Pages;

namespace TS.AppPages.ShoppingListApp.ViewModels;

// ViewModel for the main shopping lists screen 
public class ShoppingListViewModel : INotifyPropertyChanged
{
    private int _counter = 0;
    private readonly INavigation _nav;
    private readonly string _userId;
    private readonly IShoppingListService _svc;

    private const int PreviewTake = 5; // Number of preview items to fetch per list

    public ObservableCollection<ShoppingListItem> Lists { get; } = new();

    // Commands bound from the UI
    public ICommand AddListCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand OpenListCommand { get; }

    public ShoppingListViewModel(INavigation nav, string userId, IShoppingListService svc)
    {
        _nav = nav;
        _userId = userId;
        _svc = svc;

        AddListCommand = new Command(async () => await AddListAsync());
        DeleteCommand = new Command<ShoppingListItem>(async (item) => await DeleteListAsync(item));
        OpenListCommand = new Command<ShoppingListItem>(OpenList);

        // Initial load (lists + up to N items per list)
        _ = LoadListsAsync();
    }

    // Kept for compatibility (no extra work needed on appearing)
    public void OnPageAppearing() { }

    // Loads lists with up to N items per list 
    private async Task LoadListsAsync()
    {
        try
        {
            var lists = await _svc.GetListsAsync(_userId, PreviewTake);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Lists.Clear();
                foreach (var dto in lists)
                {
                    var vm = new ShoppingListItem
                    {
                        ListId = dto.ListId,
                        Name = dto.Name
                    };

                    if (dto.Items != null && dto.Items.Count > 0)
                        vm.SetPreviewItems(dto.Items.Select(i => (i.Text, i.IsChecked)));

                    Lists.Add(vm);
                }

                _counter = Lists.Count; // keeps naming counter logic as before
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadListsAsync error: {ex.Message}");
        }
    }

    // Creates a new list and persists its header via the service
    private async Task AddListAsync()
    {
        _counter++;
        var listId = Guid.NewGuid().ToString("N")[..8];
        var name = "רשימה חדשה";

        try
        {
            await _svc.CreateListAsync(_userId, listId, name);
            MainThread.BeginInvokeOnMainThread(() =>
                Lists.Add(new ShoppingListItem { ListId = listId, Name = name })
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddListAsync error: {ex.Message}");
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlert("שגיאה", ex.Message, "סגור");
        }
    }

    // Deletes a list (header + items) via the service
    private async Task DeleteListAsync(ShoppingListItem? item)
    {
        if (item is null) return;

        try
        {
            await _svc.DeleteListAsync(_userId, item.ListId);
            MainThread.BeginInvokeOnMainThread(() => Lists.Remove(item));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteListAsync error: {ex.Message}");
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlert("שגיאה", ex.Message, "סגור");
        }
    }

    // Opens the selected list for editing (full load happens in the edit page)
    private async void OpenList(ShoppingListItem? item)
    {
        if (item is null) return;
        await _nav.PushAsync(new EditList(_userId, item, _svc));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
