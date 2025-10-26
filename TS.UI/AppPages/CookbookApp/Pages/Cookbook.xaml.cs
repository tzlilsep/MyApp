using System.Threading.Tasks;

namespace TS.AppPages
{
    public partial class Cookbook : ContentPage
    {
        private readonly string _userId;
        private readonly string _idToken;

        // ✅ רפרנס כדי שנוכל לרענן כשחוזרים
        private MyCookbook _myRecipes;

        public Cookbook(string userId, string idToken)
        {
            InitializeComponent();

            _userId = userId;
            _idToken = idToken;

            // Left placeholder (עדיין ריק בשלב זה)
            var searchRecipes = new ContentView
            {
                Content = new Label
                {
                    Text = "מתכונים של אנשים אחרים",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 22,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };

            // Center = MyCookbook (כאן תהיה הלוגיקה של "המתכונים שלי")
            _myRecipes = new MyCookbook(_userId, _idToken);

            // Right placeholder (מסך הוספת מתכון – נבנה בהמשך)
            var addRecipe = new ContentView
            {
                Content = new Label
                {
                    Text = "הוספת מתכון",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 22,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                }
            };

            // אותו סדר אינדקסים: 0=Search, 1=MyRecipes, 2=AddRecipe
            RootCarousel.ItemsSource = new View[] { searchRecipes, _myRecipes, addRecipe };
            RootCarousel.Position = 1;

            RootCarousel.PositionChanged += (_, e) => UpdateBottomBarSelection(e.CurrentPosition);
            UpdateBottomBarSelection(1);

            _ = ScrollToSelectedButton(1);
        }

        // קונסטרקטור ברירת מחדל לעורך בלבד
        public Cookbook() : this(string.Empty, string.Empty) { }

        // ✅ רענון כשחוזרים לעמוד (למשל אחרי ניווט לצפייה וחזרה)
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = _myRecipes?.RefreshAsync();
        }

        // Handlers עם שמות אחידים
        private void OnTabSearchClicked(object sender, EventArgs e) => SetCarouselPosition(0);
        private void OnTabMyRecipesClicked(object sender, EventArgs e) => SetCarouselPosition(1);
        private void OnTabAddRecipeClicked(object sender, EventArgs e) => SetCarouselPosition(2);

        private async void SetCarouselPosition(int pos)
        {
            RootCarousel.Position = pos;
            UpdateBottomBarSelection(pos);

            // ✅ רענון בעת מעבר לטאב "המתכונים שלי"
            if (pos == 1)
                _ = _myRecipes?.RefreshAsync();

            await ScrollToSelectedButton(pos);
        }

        private void UpdateBottomBarSelection(int position)
        {
            var activeBg = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#AB4E52");
            var idleBg = Colors.Transparent;
            var activeText = Colors.White;
            var idleText = (Application.Current?.RequestedTheme == AppTheme.Dark) ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#AB4E52");

            void Style(Button b, bool active)
            {
                if (b is null) return;
                b.BackgroundColor = active ? activeBg : idleBg;
                b.TextColor = active ? activeText : idleText;
                b.BorderColor = (Application.Current?.RequestedTheme == AppTheme.Dark) ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#FFFFFF");
                b.BorderWidth = active ? 0 : 1;
            }

            Style(BtnTabSearch, position == 0);
            Style(BtnTabMyRecipes, position == 1);
            Style(BtnTabAddRecipe, position == 2);
        }

        private async Task ScrollToSelectedButton(int position)
        {
            if (this.FindByName<ScrollView>("BottomScroll") is not ScrollView scroll)
                return;

            Button? target = position switch
            {
                0 => BtnTabSearch,
                1 => BtnTabMyRecipes,
                2 => BtnTabAddRecipe,
                _ => null
            };
            if (target is null) return;

            await Task.Yield();

            if (scroll.Content is VisualElement content
                && scroll.Width > 0
                && content.Width > scroll.Width
                && target.Width > 0)
            {
                double targetCenter = target.X + (target.Width / 2.0);
                double desiredX = targetCenter - (scroll.Width / 2.0);
                double minX = 0;
                double maxX = Math.Max(0, content.Width - scroll.Width);
                double clampedX = Math.Max(minX, Math.Min(desiredX, maxX));
                await scroll.ScrollToAsync(clampedX, 0, true);
            }
            else
            {
                await scroll.ScrollToAsync(target, ScrollToPosition.MakeVisible, true);
            }
        }
    }
}
