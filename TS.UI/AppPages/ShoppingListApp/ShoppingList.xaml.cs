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

    // Ensure previews are (re)loaded when page becomes visible
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ShoppingListViewModel vm)
            vm.OnPageAppearing(); // triggers preview load if still missing
    }
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

    // Items used by the edit screen
    public ObservableCollection<ChecklistItem> Items { get; } = new();

    // Preview items used ONLY by the main grid (loaded lazily)
    public ObservableCollection<ChecklistItem> PreviewItems { get; } = new();

    // Preview source with safe fallback to Items when PreviewItems is empty
    public IEnumerable<ChecklistItem> FirstItems =>
        (PreviewItems.Count > 0 ? (IEnumerable<ChecklistItem>)PreviewItems : Items).Take(6);

    // Consider both collections for empty state
    public bool IsEmpty => Items.Count == 0 && PreviewItems.Count == 0;

    // Set preview items without touching the editable Items collection
    public void SetPreviewItems(IEnumerable<(string Text, bool IsChecked)> items)
    {
        PreviewItems.Clear();
        foreach (var (text, isChecked) in items)
            PreviewItems.Add(new ChecklistItem { Text = text, IsChecked = isChecked });

        // Notify bindings to refresh preview visibility and content
        OnPropertyChanged(nameof(FirstItems));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public IEnumerable<int> Placeholders { get; } = Enumerable.Range(1, 5);

    public ShoppingListItem()
    {
        // When editable items change (e.g., after returning from editor), recompute and refresh preview binding
        Items.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(FirstItems)); // ensure preview refreshes if PreviewItems is empty
        };

        // When preview items change (loaded on main screen), update bindings
        PreviewItems.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(FirstItems));
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    // Adds a new item to the editable list (used in editor)
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

    // Guard to avoid duplicate concurrent loads on Appearing
    private bool _isLoadingPreviews;

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

    // Called by page.OnAppearing to ensure previews are present
    public void OnPageAppearing()
    {
        // Only load previews if they are missing (no Items and no PreviewItems)
        if (Lists.Any(l => l.PreviewItems.Count == 0 && l.Items.Count == 0))
            _ = LoadPreviewsAsync();
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

            // Load previews after headers are on the UI
            _ = LoadPreviewsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadListsAsync error: {ex.Message}");
        }
    }

    // Optional service interface for loading preview items
    public interface IShoppingListPreviewService
    {
        Task<IEnumerable<(string Text, bool IsChecked)>> GetFirstItemsAsync(string userId, string listId, int take);
    }

    // Loads first N items for each list to show a preview on the main page
    private async Task LoadPreviewsAsync()
    {
        if (_isLoadingPreviews) return;
        _isLoadingPreviews = true; // guard

        try
        {
            var snapshot = Lists.ToList();

            foreach (var list in snapshot)
            {
                // Skip if already have any data (either Items or PreviewItems)
                if (list.PreviewItems.Count > 0 || list.Items.Count > 0)
                    continue;

                IEnumerable<(string Text, bool IsChecked)> items = Array.Empty<(string, bool)>();

                try
                {
                    items = await TryFetchPreviewAsync(_userId, list.ListId, take: 6);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadPreviewsAsync list {list.ListId} failed: {ex.Message}");
                    continue;
                }

                if (items.Any())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        list.SetPreviewItems(items);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadPreviewsAsync error: {ex.Message}");
        }
        finally
        {
            _isLoadingPreviews = false;
        }
    }

    // Tries several service signatures to fetch initial preview items without requiring service changes
    private async Task<IEnumerable<(string Text, bool IsChecked)>> TryFetchPreviewAsync(string userId, string listId, int take)
    {
        // 1) Preferred path: explicit preview interface
        if (_svc is IShoppingListPreviewService previewSvc)
            return await previewSvc.GetFirstItemsAsync(userId, listId, take);

        var svcType = _svc.GetType();

        // 2) GetFirstItemsAsync(userId, listId, take)
        var mFirst = svcType.GetMethod("GetFirstItemsAsync", new[] { typeof(string), typeof(string), typeof(int) });
        if (mFirst != null)
        {
            var result = mFirst.Invoke(_svc, new object[] { userId, listId, take });
            return await ConvertResultAsync(result);
        }

        // 3) GetItemsAsync(userId, listId)  -> Take(take)
        var mAll = svcType.GetMethod("GetItemsAsync", new[] { typeof(string), typeof(string) });
        if (mAll != null)
        {
            var result = mAll.Invoke(_svc, new object[] { userId, listId });
            var all = await ConvertResultAsync(result);
            return all.Take(take);
        }

        // 4) LoadAsync(userId, listId) -> .Items -> Take(take)
        var mLoad = svcType.GetMethod("LoadAsync", new[] { typeof(string), typeof(string) });
        if (mLoad != null)
        {
            // call LoadAsync and await the Task
            var taskObj = mLoad.Invoke(_svc, new object[] { userId, listId });
            if (taskObj is Task t)
            {
                await t.ConfigureAwait(false);
                var resProp = t.GetType().GetProperty("Result");
                var dto = resProp?.GetValue(t);
                if (dto != null)
                {
                    // read dto.Items by reflection (without referencing TS.Engine.Contracts)
                    var itemsProp = dto.GetType().GetProperty("Items");
                    var itemsObj = itemsProp?.GetValue(dto);
                    // Reuse the converter to map to (Text, IsChecked)
                    var mapped = await ConvertResultAsync(itemsObj);
                    return mapped.Take(take);
                }
            }
        }

        // No usable method found -> return empty
        return Enumerable.Empty<(string Text, bool IsChecked)>();
    }


    // Converts various possible return types to (Text, IsChecked)
    private static async Task<IEnumerable<(string Text, bool IsChecked)>> ConvertResultAsync(object? result)
    {
        if (result is null) return Enumerable.Empty<(string, bool)>();

        // Await Task if needed
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var resProp = task.GetType().GetProperty("Result");
            result = resProp?.GetValue(task);
            if (result is null) return Enumerable.Empty<(string, bool)>();
        }

        if (result is System.Collections.IEnumerable seq)
        {
            var list = new List<(string Text, bool IsChecked)>();
            foreach (var item in seq)
            {
                if (item is null) continue;
                var t = item.GetType();

                // Tuple (Text, IsChecked)
                if (t.FullName?.StartsWith("System.ValueTuple") == true)
                {
                    var text = t.GetField("Item1")?.GetValue(item)?.ToString() ?? string.Empty;
                    var isChecked = t.GetField("Item2")?.GetValue(item) as bool? ?? false;
                    if (!string.IsNullOrWhiteSpace(text))
                        list.Add((text, isChecked));
                    continue;
                }

                // Object with Text/Title/Name and IsChecked/Done/Checked
                string textVal =
                    t.GetProperty("Text")?.GetValue(item)?.ToString() ??
                    t.GetProperty("Title")?.GetValue(item)?.ToString() ??
                    t.GetProperty("Name")?.GetValue(item)?.ToString() ??
                    string.Empty;

                bool isCheckedVal =
                    t.GetProperty("IsChecked")?.GetValue(item) as bool? ??
                    t.GetProperty("Checked")?.GetValue(item) as bool? ??
                    t.GetProperty("Done")?.GetValue(item) as bool? ??
                    false;

                if (!string.IsNullOrWhiteSpace(textVal))
                    list.Add((textVal, isCheckedVal));
            }
            return list;
        }

        return Enumerable.Empty<(string, bool)>();
    }

    // Creates a new list and persists its header via the service
    private async Task AddListAsync()
    {
        _counter++;
        var listId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var name = $"רשימה חדשה";

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
