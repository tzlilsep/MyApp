using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TS.AppPages.CookbookApp.UiModels
{
    // מודל תצוגה קליל לכרטיס ברשת
    public sealed class RecipeCard : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private int _prepMinutes;
        private int _totalMinutes;
        private string? _imageUrl;

        public string RecipeId { get; }
        public string Title { get => _title; set { if (_title != value) { _title = value; OnPropertyChanged(); } } }
        public int PrepMinutes { get => _prepMinutes; set { if (_prepMinutes != value) { _prepMinutes = value; OnPropertyChanged(); } } }
        public int TotalMinutes { get => _totalMinutes; set { if (_totalMinutes != value) { _totalMinutes = value; OnPropertyChanged(); } } }
        public string? ImageUrl { get => _imageUrl; set { if (_imageUrl != value) { _imageUrl = value; OnPropertyChanged(); } } }

        public RecipeCard(string recipeId, string title, int prepMinutes, int totalMinutes, string? imageUrl)
        {
            RecipeId = recipeId;
            _title = title ?? string.Empty;
            _prepMinutes = prepMinutes;
            _totalMinutes = totalMinutes;
            _imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
