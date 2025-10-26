using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TS.AppPages.CookbookApp.UiModels;
using TS.Engine.Abstractions;

namespace TS.AppPages.CookbookApp.ViewModels
{
    /// ViewModel לטאב "המתכונים שלי": טוען תקצירים מהענן ומחזיק רשימת כרטיסים.
    public sealed class MyRecipesViewModel
    {
        private readonly string _userId;
        private readonly IRecipesService _svc;

        public ObservableCollection<RecipeCard> Recipes { get; } = new();
        public bool IsBusy { get; private set; }
        public ICommand RefreshCommand { get; }

        public MyRecipesViewModel(string userId, IRecipesService svc)
        {
            _userId = userId;
            _svc = svc;
            RefreshCommand = new Command(async () => await LoadAsync());
        }

        public async Task LoadAsync(int take = 50, int skip = 0)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var list = await _svc.GetMyRecipesAsync(_userId, take, skip);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Recipes.Clear();
                    foreach (var r in list)
                    {
                        Recipes.Add(new RecipeCard(
                            recipeId: r.RecipeId,
                            title: r.Title,
                            prepMinutes: r.PrepMinutes,
                            totalMinutes: r.TotalMinutes,
                            imageUrl: string.IsNullOrWhiteSpace(r.ImageUrl)
                                ? "https://placehold.co/600x400?text=Recipe"
                                : r.ImageUrl
                        ));
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MyRecipesViewModel.LoadAsync error: {ex.Message}");
                // אם תרצי הודעה למשתמש: אפשר להרים אירוע/MessageCenter או להשתמש ב-Shell.Current.DisplayAlert מבחוץ
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
