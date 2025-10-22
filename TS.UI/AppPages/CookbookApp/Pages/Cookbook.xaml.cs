using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TS.AppPages;

public partial class Cookbook : ContentPage
{
    private readonly string _userId;
    private readonly string _idToken;

    public ObservableCollection<RecipeCard> Recipes { get; } = new();

    public ICommand OpenRecipeCommand { get; }

    public Cookbook(string userId, string idToken)
    {
        InitializeComponent();

        _userId = userId;
        _idToken = idToken;
        BindingContext = this;

        // דמו
        Recipes.Add(new RecipeCard("קציצות דגים", 20, 45, "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("פוקאצ׳ה", 20, 120, "https://images.unsplash.com/photo-1546549039-49bf2b5be0c4?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("סלט יווני", 10, 10, "https://images.unsplash.com/photo-1540420773420-3366772f4999?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("מרק עדשים", 15, 40, "https://images.unsplash.com/photo-1543363136-3bf8f9d394b1?q=80&w=1200&auto=format&fit=crop"));

        OpenRecipeCommand = new Command<RecipeCard>(async (card) =>
        {
            if (card is null) return;
            await DisplayAlert("Recipe", card.Title, "Close");
        });

        // מסכים
        var addRecipe = new ContentView
        {
            Content = new Label
            {
                Text = "הוספת מתכון (soon)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 22,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var gallery = new MyCookbook { BindingContext = this };

        var search = new ContentView
        {
            Content = new Label
            {
                Text = "חיפוש מתכונים (soon)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 22,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var tools = new ContentView
        {
            Content = new Label
            {
                Text = "כלים (soon)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        var more = new ContentView
        {
            Content = new Label
            {
                Text = "בהמשך (soon)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        // 🧭 אצלך הכול הפוך, אז אנחנו הופכים גם את הסדר
        // הוסף מתכון -> בצד שמאל בפועל
        // חפש מתכונים -> בימין בפועל
        RootCarousel.ItemsSource = new View[] { addRecipe, gallery, search, tools, more };
        RootCarousel.Position = 1; // "המתכונים שלי" ברירת מחדל

        RootCarousel.PositionChanged += (_, e) => UpdateBottomBarSelection(e.CurrentPosition);
        UpdateBottomBarSelection(1);
        _ = ScrollToSelectedButton(1);
    }

    public Cookbook() : this(string.Empty, string.Empty) { }

    // בגלל שהכול הפוך, אנחנו ממפים הפוך:
    private void OnTabAddRecipeClicked(object sender, EventArgs e) => SetCarouselPosition(0); // שמאל בפועל
    private void OnTabMyRecipesClicked(object sender, EventArgs e) => SetCarouselPosition(1); // מרכז
    private void OnTabSearchClicked(object sender, EventArgs e) => SetCarouselPosition(2);    // ימין בפועל
    private void OnTabToolsClicked(object sender, EventArgs e) => SetCarouselPosition(3);
    private void OnTabMoreClicked(object sender, EventArgs e) => SetCarouselPosition(4);

    private async void SetCarouselPosition(int pos)
    {
        RootCarousel.Position = pos;
        UpdateBottomBarSelection(pos);
        await ScrollToSelectedButton(pos);
    }

    private void UpdateBottomBarSelection(int position)
    {
        var activeBg = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#AB4E52");
        var idleBg = Colors.Transparent;
        var activeText = Colors.White;
        var idleText = (Application.Current?.RequestedTheme == AppTheme.Dark)
            ? Color.FromArgb("#FFFFFF")
            : Color.FromArgb("#AB4E52");

        void Style(Button b, bool active)
        {
            if (b is null) return;
            b.BackgroundColor = active ? activeBg : idleBg;
            b.TextColor = active ? activeText : idleText;
            b.BorderColor = (Application.Current?.RequestedTheme == AppTheme.Dark)
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#FFFFFF");
            b.BorderWidth = active ? 0 : 1;
        }

        // אותו סדר כמו הקרוסלה
        Style(BtnTabAddRecipe, position == 0);
        Style(BtnTabMyRecipes, position == 1);
        Style(BtnTabSearch, position == 2);
        Style(BtnTabTools, position == 3);
        Style(BtnTabMore, position == 4);
    }

    private async Task ScrollToSelectedButton(int position)
    {
        var scroll = this.FindByName<ScrollView>("BottomScroll");
        if (scroll == null) return;

        Button target = position switch
        {
            0 => BtnTabAddRecipe,
            1 => BtnTabMyRecipes,
            2 => BtnTabSearch,
            3 => BtnTabTools,
            4 => BtnTabMore,
            _ => null
        };
        if (target == null) return;

        await Task.Yield();
        var content = scroll.Content as VisualElement;
        if (content == null) return;

        double viewport = scroll.Width;
        double contentW = content.Width;
        if (viewport <= 0 || contentW <= viewport)
        {
            await scroll.ScrollToAsync(0, 0, false);
            return;
        }

        // 🧭 חישוב נכון לפי RTL - ממרכז הכפתור מהקצה הימני
        double targetRightEdge = contentW - (target.X + target.Width);
        double targetCenter = targetRightEdge + (target.Width / 2.0);
        double desiredX = targetCenter - (viewport / 2.0);

        // ✅ מגבילים את התנועה כך שאין חלל ריק
        double minX = 0;
        double maxX = Math.Max(0, contentW - viewport);
        double clampedX = Math.Max(minX, Math.Min(desiredX, maxX));

        await scroll.ScrollToAsync(clampedX, 0, true);
    }




    public record RecipeCard(string Title, int PrepMinutes, int TotalMinutes, string ImageUrl);
}