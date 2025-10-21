using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TS.AppPages.ShoppingListApp.UiModels;


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
        (Items.Count > 0 ? (IEnumerable<ChecklistItem>)Items : PreviewItems).Take(6);


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
