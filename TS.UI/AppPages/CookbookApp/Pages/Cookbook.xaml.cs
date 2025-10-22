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

        // Demo data
        Recipes.Add(new RecipeCard("קציצות דגים", 20, 45, "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("פוקאצ׳ה", 20, 120, "https://images.unsplash.com/photo-1546549039-49bf2b5be0c4?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("סלט יווני", 10, 10, "https://images.unsplash.com/photo-1540420773420-3366772f4999?q=80&w=1200&auto=format&fit=crop"));
        Recipes.Add(new RecipeCard("מרק עדשים", 15, 40, "https://images.unsplash.com/photo-1543363136-3bf8f9d394b1?q=80&w=1200&auto=format&fit=crop"));

        // Command
        OpenRecipeCommand = new Command<RecipeCard>(async (card) =>
        {
            if (card is null) return;
            await DisplayAlert("Recipe", card.Title, "Close");
        });

        // Left placeholder
        var left = new ContentView
        {
            Content = new Label
            {
                Text = "מסך שמאל (למשל: קטגוריות/אוספים)",
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Opacity = 0.6
            }
        };

        // Center = MyCookbook (ContentView!)
        var gallery = new MyCookbook { BindingContext = this };

        // Right placeholder
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

        // Extra placeholders
        var tools = new ContentView
        {
            Content = new Label
            {
                Text = "כרטיסייה 4 (soon)",
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
                Text = "כרטיסייה 5 (soon)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        // אותו סדר אינדקסים: 0=Search, 1=MyRecipes, 2=AddRecipe, 3=Tools, 4=More
        RootCarousel.ItemsSource = new View[] { left, gallery, addRecipe, tools, more };
        RootCarousel.Position = 1;

        RootCarousel.PositionChanged += (_, e) => UpdateBottomBarSelection(e.CurrentPosition);
        UpdateBottomBarSelection(1);

        _ = ScrollToSelectedButton(1);
    }

    public Cookbook() : this(string.Empty, string.Empty) { }

    // Handlers עם שמות אחידים
    private void OnTabSearchClicked(object sender, EventArgs e) => SetCarouselPosition(0); // היה OnLeftTabClicked
    private void OnTabMyRecipesClicked(object sender, EventArgs e) => SetCarouselPosition(1); // היה OnCenterTabClicked
    private void OnTabAddRecipeClicked(object sender, EventArgs e) => SetCarouselPosition(2); // היה OnRightTabClicked
    private void OnTabToolsClicked(object sender, EventArgs e) => SetCarouselPosition(3); // היה OnExtra1TabClicked
    private void OnTabMoreClicked(object sender, EventArgs e) => SetCarouselPosition(4); // היה OnExtra2TabClicked

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
        Style(BtnTabTools, position == 3);
        Style(BtnTabMore, position == 4);
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
            3 => BtnTabTools,
            4 => BtnTabMore,
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

public record RecipeCard(string Title, int PrepMinutes, int TotalMinutes, string ImageUrl);
