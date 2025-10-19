using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

        // In designer mode, avoid setting BindingContext to prevent design-time errors
        if (svc != null)
            BindingContext = new ShoppingListViewModel(this.Navigation, _userId, svc);
    }

    // Parameterless constructor for designer only (not for runtime use)
    public ShoppingList() : this(string.Empty, null!) { }
}

/* Inverse boolean converter for XAML bindings */
public sealed class BoolInverseConverter : IValueConverter
{
    public static readonly BoolInverseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is bool b ? !b : value;
}

/* UI checklist item with change notification */
public class ChecklistItem : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private bool _isChecked;

    public string Text
    {
        get => _text;
        set { if (_text != value) { _text = value; OnPropertyChanged(); } }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked != value) { _isChecked = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/* UI model of a shopping list (title + items) */
public class ShoppingListItem : INotifyPropertyChanged
{
    private string _name = string.Empty;

    public string ListId { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public ObservableCollection<ChecklistItem> Items { get; } = new();

    // Convenience properties for UI
    public IEnumerable<ChecklistItem> FirstItems => Items.Take(5);
    public bool IsEmpty => Items.Count == 0;
    public IEnumerable<int> Placeholders { get; } = Enumerable.Range(1, 5);

    public ShoppingListItem()
    {
        // Recompute derived properties when the collection changes
        Items.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(FirstItems));
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    // Adds a new item to the list (UI-level only)
    public void AddItem(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            Items.Add(new ChecklistItem { Text = text.Trim(), IsChecked = false });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/* ViewModel for the main shopping lists screen (depends only on the service interface) */
public class ShoppingListViewModel : INotifyPropertyChanged
{
    private int _counter = 0;
    private readonly INavigation _nav;
    private readonly string _userId;
    private readonly IShoppingListService _svc;

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

        // Fire-and-forget initial load
        _ = LoadListsAsync();
    }

    // Loads list headers from the service (cloud-backed)
    private async Task LoadListsAsync()
    {
        try
        {
            var lists = await _svc.GetListsAsync(_userId);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Lists.Clear();
                foreach (var (listId, name) in lists)
                    Lists.Add(new ShoppingListItem { ListId = listId, Name = name });

                _counter = Lists.Count; // Keep running counter for naming
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
        var listId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var name = $"רשימה חדשה {_counter}";

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
                await page.DisplayAlert("שגיאה", ex.Message, "?גור");
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

    // Opens the selected list for editing
    private async void OpenList(ShoppingListItem? item)
    {
        if (item is null) return;
        await _nav.PushAsync(new EditList(_userId, item, _svc));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
