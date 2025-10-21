using TS.Engine.Contracts;

namespace TS.Engine.Abstractions
{
    public interface IShoppingListService
    {
        // Returns all shopping lists for a given user, each with up to 'take' items
        Task<IReadOnlyList<ShoppingListDto>> GetListsAsync(string userId, int take);

        Task CreateListAsync(string userId, string listId, string name);
        Task DeleteListAsync(string userId, string listId);

        // Loads a single list with all of its items
        Task<ShoppingListDto> LoadAsync(string userId, string listId);

        Task SaveAsync(ShoppingListDto list);
    }
}
