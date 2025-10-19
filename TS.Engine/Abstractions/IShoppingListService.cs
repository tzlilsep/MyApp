using TS.Engine.Contracts;

namespace TS.Engine.Abstractions
{
    public interface IShoppingListService
    {
        // Returns all shopping lists for a given user (IDs and names only)
        Task<IReadOnlyList<(string ListId, string Name)>> GetListsAsync(string userId);

        // Creates a new list entry
        Task CreateListAsync(string userId, string listId, string name);

        // Deletes an entire list by its ID
        Task DeleteListAsync(string userId, string listId);

        // Optional: validate list existence (may throw if not found)
        Task ShoppingListExistsOrThrow(string userId, string listId);

        // Loads full list data (name + items)
        Task<ShoppingListDto> LoadAsync(string userId, string listId);

        // Saves the given list data to storage
        Task SaveAsync(ShoppingListDto list);
    }
}
