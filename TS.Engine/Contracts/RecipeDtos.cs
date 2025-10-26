// TS.Engine.Contracts/RecipeDtos.cs
namespace TS.Engine.Contracts
{
    public sealed record IngredientDto(string Text);
    public sealed record StepDto(string Text, string? ImageUrl);

    public sealed record RecipeSummaryDto(
        string RecipeId,
        string Title,
        int PrepMinutes,
        int TotalMinutes,
        string? ImageUrl
    );

    // הוספנו UserId
    public sealed record RecipeDetailDto(
        string UserId,
        string RecipeId,
        string Title,
        int PrepMinutes,
        int TotalMinutes,
        int? Servings,
        string? ImageUrl,
        IReadOnlyList<IngredientDto> Ingredients,
        IReadOnlyList<StepDto> Steps,
        string? Notes
    );
}
