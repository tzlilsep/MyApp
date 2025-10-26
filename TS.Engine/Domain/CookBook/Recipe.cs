using System.Collections.Generic;

namespace TS.Engine.Domain.CookBook
{
    /// <summary>אגרגט מתכון בסיסי: כותרת, זמנים, מנות, תמונה, מצרכים ושלבים.</summary>
    public sealed class Recipe
    {
        public RecipeId Id { get; }
        public string UserId { get; }
        public string Title { get; private set; }
        public int PrepMinutes { get; private set; }
        public int TotalMinutes { get; private set; }
        public int? Servings { get; private set; }
        public string? ImageUrl { get; private set; }

        private readonly List<Ingredient> _ingredients = new();
        private readonly List<Step> _steps = new();

        public IReadOnlyList<Ingredient> Ingredients => _ingredients;
        public IReadOnlyList<Step> Steps => _steps;

        public Recipe(string userId, RecipeId id, string title, int prepMinutes, int totalMinutes,
                      int? servings = null, string? imageUrl = null)
        {
            UserId = (userId ?? string.Empty).Trim();
            Id = id;
            Title = (title ?? string.Empty).Trim();
            PrepMinutes = prepMinutes >= 0 ? prepMinutes : 0;
            TotalMinutes = totalMinutes >= 0 ? totalMinutes : 0;
            Servings = servings is > 0 ? servings : null;
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        }

        // ----- עריכות קטנות -----
        public void Rename(string title)
        {
            var t = (title ?? string.Empty).Trim();
            if (t.Length > 0) Title = t;
        }

        public void SetTimes(int prepMinutes, int totalMinutes)
        {
            PrepMinutes = prepMinutes >= 0 ? prepMinutes : 0;
            TotalMinutes = totalMinutes >= 0 ? totalMinutes : 0;
        }

        public void SetServings(int? servings) => Servings = servings is > 0 ? servings : null;
        public void SetImage(string? url) => ImageUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();

        // ----- מצרכים -----
        public void AddIngredient(string text) { var t = (text ?? "").Trim(); if (t.Length > 0) _ingredients.Add(new Ingredient(t)); }
        public void RemoveIngredientAt(int index) { if (index >= 0 && index < _ingredients.Count) _ingredients.RemoveAt(index); }
        public void MoveIngredient(int from, int to)
        {
            if (from == to) return;
            if (from < 0 || from >= _ingredients.Count) return;
            to = Math.Clamp(to, 0, _ingredients.Count - 1);
            var item = _ingredients[from]; _ingredients.RemoveAt(from); _ingredients.Insert(to, item);
        }

        // ----- שלבים -----
        public void AddStep(string text)
        {
            var t = (text ?? "").Trim();
            if (t.Length > 0)
                _steps.Add(new Step(t));
        }
        public void RemoveStepAt(int index) { if (index >= 0 && index < _steps.Count) _steps.RemoveAt(index); }
        public void MoveStep(int from, int to)
        {
            if (from == to) return;
            if (from < 0 || from >= _steps.Count) return;
            to = Math.Clamp(to, 0, _steps.Count - 1);
            var s = _steps[from]; _steps.RemoveAt(from); _steps.Insert(to, s);
        }
    }
}
