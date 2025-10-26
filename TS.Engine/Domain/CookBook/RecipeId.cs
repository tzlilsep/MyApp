namespace TS.Engine.Domain.CookBook
{
    /// <summary>Value object למזהה מתכון</summary>
    public readonly record struct RecipeId(string Value)
    {
        public static RecipeId New() => new(Guid.NewGuid().ToString("N"));
        public override string ToString() => Value;
    }
}
