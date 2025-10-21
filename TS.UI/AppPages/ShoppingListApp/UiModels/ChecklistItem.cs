using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TS.AppPages.ShoppingListApp.UiModels;


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