namespace TS.Engine.Domain.CookBook
{
    /// <summary>Projection קליל לרשימת כרטיסים/קטלוג</summary>
    public sealed class RecipeSummary
    {
        public RecipeId Id { get; }
        public string Title { get; }
        public int PrepMinutes { get; }
        public int TotalMinutes { get; }
        public string? ImageUrl { get; }

        public RecipeSummary(RecipeId id, string title, int prepMinutes, int totalMinutes, string? imageUrl)
        {
            Id = id;
            Title = title;
            PrepMinutes = prepMinutes;
            TotalMinutes = totalMinutes;
            ImageUrl = imageUrl;
        }
    }
}
