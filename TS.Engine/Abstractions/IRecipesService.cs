using System.Collections.Generic;
using System.Threading.Tasks;
using TS.Engine.Contracts;

namespace TS.Engine.Abstractions
{
    /// CRUD בסיסי + שאילתות להצגה
    public interface IRecipesService
    {
        Task<IReadOnlyList<RecipeSummaryDto>> GetMyRecipesAsync(string userId, int take = 20, int skip = 0);

        Task<RecipeDetailDto> GetRecipeAsync(string userId, string recipeId);

        Task CreateRecipeAsync(RecipeDetailDto recipe);

        Task UpdateRecipeAsync(RecipeDetailDto recipe);

        Task DeleteRecipeAsync(string userId, string recipeId);
    }

    public interface IRecipesServiceFactory
    {
        IRecipesService Create(string idToken);
    }
}
