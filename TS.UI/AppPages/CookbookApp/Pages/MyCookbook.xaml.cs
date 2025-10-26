using System.Windows.Input;
using TS.AppPages.CookbookApp.ViewModels;
using TS.AppPages.CookbookApp.UiModels;
using TS.Engine.Abstractions;
using TS.AWS.Services;

namespace TS.AppPages
{
    public partial class MyCookbook : ContentView
    {
        private readonly MyRecipesViewModel _vm;

        public ICommand OpenRecipeCommand { get; }

        public MyCookbook(string userId, string idToken)
        {
            InitializeComponent();

            IRecipesService svc = new AwsRecipesService(idToken);
            _vm = new MyRecipesViewModel(userId, svc);
            BindingContext = _vm;

            // ניווט למסך מתכון
            OpenRecipeCommand = new Command<RecipeCard>(async (card) =>
            {
                if (card == null) return;
                await Shell.Current.Navigation.PushAsync(new RecipeViewPage(card.Title));
            });

            _ = _vm.LoadAsync();
        }

        public MyCookbook() : this(string.Empty, string.Empty) { }

        // ✅ תוספת: רענון חיצוני מהדף האב
        public Task RefreshAsync() => _vm.LoadAsync();
    }
}
